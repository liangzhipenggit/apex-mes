using Mes.Common;
using Mes.DAL;
using Mes.Models;
using SqlSugar;

namespace Mes.BLL;

public class UserService
{
    public async Task<ApiResult<PageResult<UserListItem>>> QueryAsync(UserQuery query)
    {
        try
        {
            var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
            var keyword = query.Keyword?.Trim();

            if (AppHost.DemoMode)
            {
                var db = LocalDbStore.GetSnapshot();
                IEnumerable<SysUser> rows = db.Users.Where(u => !u.IsDeleted);
                if (!string.IsNullOrEmpty(keyword))
                {
                    rows = rows.Where(u =>
                        u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || u.RealName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                }

                if (query.Enabled.HasValue)
                    rows = rows.Where(u => u.Enabled == query.Enabled.Value);

                var list = rows.OrderByDescending(u => u.Id).ToList();
                var total = list.Count;
                var page = list.Skip((pageIndex - 1) * pageSize).Take(pageSize)
                    .Select(u => ToListItem(u, db))
                    .ToList();
                return ApiResult<PageResult<UserListItem>>.Ok(new PageResult<UserListItem>(page, total));
            }

            RefAsync<int> totalRows = 0;
            var users = await SqlSugarSetup.Db.Queryable<SysUser>()
                .Where(u => u.IsDeleted == false)
                .WhereIF(!string.IsNullOrEmpty(keyword), u => u.UserName.Contains(keyword!) || u.RealName.Contains(keyword!))
                .WhereIF(query.Enabled.HasValue, u => u.Enabled == query.Enabled!.Value)
                .OrderByDescending(u => u.Id)
                .ToPageListAsync(pageIndex, pageSize, totalRows);

            var ids = users.Select(u => u.Id).ToList();
            var links = ids.Count == 0
                ? []
                : await SqlSugarSetup.Db.Queryable<SysUserRole>()
                    .InnerJoin<SysRole>((ur, r) => ur.RoleId == r.Id)
                    .Where((ur, r) => ids.Contains(ur.UserId) && r.IsDeleted == false)
                    .Select((ur, r) => new { ur.UserId, r.Id, r.RoleName })
                    .ToListAsync();

            var pageItems = users.Select(u =>
            {
                var roles = links.Where(x => x.UserId == u.Id).ToList();
                return new UserListItem
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    RealName = u.RealName,
                    Enabled = u.Enabled,
                    RoleIds = roles.Select(x => x.Id).ToList(),
                    RoleNames = string.Join("、", roles.Select(x => x.RoleName))
                };
            }).ToList();

            return ApiResult<PageResult<UserListItem>>.Ok(new PageResult<UserListItem>(pageItems, totalRows));
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "查询用户");
            return ApiResult<PageResult<UserListItem>>.Fail("查询用户失败，详细信息已写入日志。");
        }
    }

    public async Task<ApiResult<UserSaveModel>> GetAsync(long id)
    {
        try
        {
            if (AppHost.DemoMode)
            {
                var db = LocalDbStore.GetSnapshot();
                var user = db.Users.FirstOrDefault(u => u.Id == id && !u.IsDeleted);
                if (user == null)
                    return ApiResult<UserSaveModel>.Fail("用户不存在或已删除。");

                return ApiResult<UserSaveModel>.Ok(new UserSaveModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    RealName = user.RealName,
                    Enabled = user.Enabled,
                    RoleIds = db.UserRoles.Where(ur => ur.UserId == id).Select(ur => ur.RoleId).ToList()
                });
            }

            var entity = await SqlSugarSetup.Db.Queryable<SysUser>()
                .FirstAsync(u => u.Id == id && u.IsDeleted == false);
            if (entity == null)
                return ApiResult<UserSaveModel>.Fail("用户不存在或已删除。");

            var roleIds = await SqlSugarSetup.Db.Queryable<SysUserRole>()
                .Where(ur => ur.UserId == id)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            return ApiResult<UserSaveModel>.Ok(new UserSaveModel
            {
                Id = entity.Id,
                UserName = entity.UserName,
                RealName = entity.RealName,
                Enabled = entity.Enabled,
                RoleIds = roleIds
            });
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "读取用户");
            return ApiResult<UserSaveModel>.Fail("读取用户失败，详细信息已写入日志。");
        }
    }

    public async Task<ApiResult> SaveAsync(UserSaveModel model)
    {
        try
        {
            model.UserName = model.UserName.Trim();
            model.RealName = model.RealName.Trim();
            if (model.UserName.Length == 0 || model.RealName.Length == 0)
                return ApiResult.Fail("用户名和姓名不能为空。");

            if (model.Id == 0 && string.IsNullOrWhiteSpace(model.Password))
                return ApiResult.Fail("新建用户必须设置密码。");

            if (AppHost.DemoMode)
            {
                return LocalDbStore.Update(db =>
                {
                    var dup = db.Users.Any(u => !u.IsDeleted
                        && u.UserName.Equals(model.UserName, StringComparison.OrdinalIgnoreCase)
                        && u.Id != model.Id);
                    if (dup)
                        return ApiResult.Fail("用户名已存在。");

                    SysUser user;
                    if (model.Id == 0)
                    {
                        user = new SysUser
                        {
                            Id = db.NextUserId++,
                            UserName = model.UserName,
                            RealName = model.RealName,
                            PasswordHash = PasswordHasher.Hash(model.Password!),
                            Enabled = model.Enabled,
                            CreateTime = DateTime.Now,
                            CreateUserId = AppSession.UserId
                        };
                        db.Users.Add(user);
                    }
                    else
                    {
                        user = db.Users.FirstOrDefault(u => u.Id == model.Id && !u.IsDeleted)!;
                        if (user == null)
                            return ApiResult.Fail("用户不存在或已删除。");
                        user.UserName = model.UserName;
                        user.RealName = model.RealName;
                        user.Enabled = model.Enabled;
                        user.UpdateTime = DateTime.Now;
                        user.UpdateUserId = AppSession.UserId;
                        if (!string.IsNullOrWhiteSpace(model.Password))
                            user.PasswordHash = PasswordHasher.Hash(model.Password);
                    }

                    db.UserRoles.RemoveAll(ur => ur.UserId == user.Id);
                    foreach (var roleId in model.RoleIds.Distinct())
                    {
                        if (!db.Roles.Any(r => r.Id == roleId && !r.IsDeleted))
                            continue;
                        db.UserRoles.Add(new SysUserRole
                        {
                            Id = db.NextUserRoleId++,
                            UserId = user.Id,
                            RoleId = roleId
                        });
                    }

                    return ApiResult.Ok("保存成功。");
                });
            }

            var dbClient = SqlSugarSetup.Db;
            var exists = await dbClient.Queryable<SysUser>()
                .AnyAsync(u => u.IsDeleted == false && u.UserName == model.UserName && u.Id != model.Id);
            if (exists)
                return ApiResult.Fail("用户名已存在。");

            await DbWrite.RunAsync(async tran =>
            {
                long userId;
                if (model.Id == 0)
                {
                    var entity = new SysUser
                    {
                        UserName = model.UserName,
                        RealName = model.RealName,
                        PasswordHash = PasswordHasher.Hash(model.Password!),
                        Enabled = model.Enabled,
                        CreateTime = DateTime.Now,
                        CreateUserId = AppSession.UserId
                    };
                    userId = await tran.Insertable(entity).ExecuteReturnBigIdentityAsync();
                }
                else
                {
                    var entity = await tran.Queryable<SysUser>().FirstAsync(u => u.Id == model.Id && u.IsDeleted == false);
                    if (entity == null)
                        throw new InvalidOperationException("用户不存在或已删除。");

                    entity.UserName = model.UserName;
                    entity.RealName = model.RealName;
                    entity.Enabled = model.Enabled;
                    entity.UpdateTime = DateTime.Now;
                    entity.UpdateUserId = AppSession.UserId;
                    if (!string.IsNullOrWhiteSpace(model.Password))
                        entity.PasswordHash = PasswordHasher.Hash(model.Password);
                    await tran.Updateable(entity).ExecuteCommandAsync();
                    userId = entity.Id;
                    await tran.Deleteable<SysUserRole>().Where(ur => ur.UserId == userId).ExecuteCommandAsync();
                }

                if (model.RoleIds.Count > 0)
                {
                    var links = model.RoleIds.Distinct().Select(rid => new SysUserRole { UserId = userId, RoleId = rid }).ToList();
                    await tran.Insertable(links).ExecuteCommandAsync();
                }
            });
            return ApiResult.Ok("保存成功。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "保存用户");
            if (ex is InvalidOperationException)
                return ApiResult.Fail(ex.Message);
            return ApiResult.Fail("保存用户失败，详细信息已写入日志。");
        }
    }

    public async Task<ApiResult> DeleteAsync(long id)
    {
        try
        {
            if (id == AppSession.UserId)
                return ApiResult.Fail("不能删除当前登录用户。");

            if (AppHost.DemoMode)
            {
                return LocalDbStore.Update(db =>
                {
                    var user = db.Users.FirstOrDefault(u => u.Id == id && !u.IsDeleted);
                    if (user == null)
                        return ApiResult.Fail("用户不存在或已删除。");
                    user.IsDeleted = true;
                    user.UpdateTime = DateTime.Now;
                    db.UserRoles.RemoveAll(ur => ur.UserId == id);
                    return ApiResult.Ok("删除成功。");
                });
            }

            var entity = await SqlSugarSetup.Db.Queryable<SysUser>().FirstAsync(u => u.Id == id && u.IsDeleted == false);
            if (entity == null)
                return ApiResult.Fail("用户不存在或已删除。");

            entity.IsDeleted = true;
            entity.UpdateTime = DateTime.Now;
            entity.UpdateUserId = AppSession.UserId;
            await SqlSugarSetup.Db.Updateable(entity).ExecuteCommandAsync();
            await SqlSugarSetup.Db.Deleteable<SysUserRole>().Where(ur => ur.UserId == id).ExecuteCommandAsync();
            return ApiResult.Ok("删除成功。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "删除用户");
            return ApiResult.Fail("删除用户失败，详细信息已写入日志。");
        }
    }

    private static UserListItem ToListItem(SysUser user, LocalDbSnapshot db)
    {
        var roles = db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(db.Roles.Where(r => !r.IsDeleted), ur => ur.RoleId, r => r.Id, (_, r) => r)
            .ToList();
        return new UserListItem
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Enabled = user.Enabled,
            RoleIds = roles.Select(r => r.Id).ToList(),
            RoleNames = string.Join("、", roles.Select(r => r.RoleName))
        };
    }
}
