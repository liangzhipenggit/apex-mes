using Mes.Common;
using Mes.DAL;
using Mes.Models;
using SqlSugar;

namespace Mes.BLL;

public class RoleService
{
    public async Task<ApiResult<PageResult<RoleListItem>>> QueryAsync(RoleQuery query)
    {
        try
        {
            var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
            var keyword = query.Keyword?.Trim();

            if (AppHost.DemoMode)
            {
                var db = LocalDbStore.GetSnapshot();
                IEnumerable<SysRole> rows = db.Roles.Where(r => !r.IsDeleted);
                if (!string.IsNullOrEmpty(keyword))
                {
                    rows = rows.Where(r =>
                        r.RoleName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || r.RoleCode.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                }

                if (query.Enabled.HasValue)
                    rows = rows.Where(r => r.Enabled == query.Enabled.Value);

                var list = rows.OrderByDescending(r => r.Id).ToList();
                var total = list.Count;
                var page = list.Skip((pageIndex - 1) * pageSize).Take(pageSize)
                    .Select(r => new RoleListItem
                    {
                        Id = r.Id,
                        RoleName = r.RoleName,
                        RoleCode = r.RoleCode,
                        Enabled = r.Enabled,
                        PermissionCount = db.RolePermissions.Count(rp => rp.RoleId == r.Id)
                    }).ToList();
                return ApiResult<PageResult<RoleListItem>>.Ok(new PageResult<RoleListItem>(page, total));
            }

            RefAsync<int> totalRows = 0;
            var roles = await SqlSugarSetup.Db.Queryable<SysRole>()
                .Where(r => r.IsDeleted == false)
                .WhereIF(!string.IsNullOrEmpty(keyword), r => r.RoleName.Contains(keyword!) || r.RoleCode.Contains(keyword!))
                .WhereIF(query.Enabled.HasValue, r => r.Enabled == query.Enabled!.Value)
                .OrderByDescending(r => r.Id)
                .ToPageListAsync(pageIndex, pageSize, totalRows);

            var ids = roles.Select(r => r.Id).ToList();
            var counts = ids.Count == 0
                ? new Dictionary<long, int>()
                : (await SqlSugarSetup.Db.Queryable<SysRolePermission>()
                    .Where(rp => ids.Contains(rp.RoleId))
                    .GroupBy(rp => rp.RoleId)
                    .Select(rp => new { rp.RoleId, Cnt = SqlFunc.AggregateCount(rp.Id) })
                    .ToListAsync())
                .ToDictionary(x => x.RoleId, x => x.Cnt);

            var pageItems = roles.Select(r => new RoleListItem
            {
                Id = r.Id,
                RoleName = r.RoleName,
                RoleCode = r.RoleCode,
                Enabled = r.Enabled,
                PermissionCount = counts.GetValueOrDefault(r.Id)
            }).ToList();

            return ApiResult<PageResult<RoleListItem>>.Ok(new PageResult<RoleListItem>(pageItems, totalRows));
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "查询角色");
            return ApiResult<PageResult<RoleListItem>>.Fail("查询角色失败，详细信息已写入本地日志。");
        }
    }

    public async Task<ApiResult<List<SysRole>>> ListEnabledAsync()
    {
        try
        {
            if (AppHost.DemoMode)
            {
                var db = LocalDbStore.GetSnapshot();
                var list = db.Roles.Where(r => !r.IsDeleted && r.Enabled).OrderBy(r => r.Id).ToList();
                return ApiResult<List<SysRole>>.Ok(list);
            }

            var roles = await SqlSugarSetup.Db.Queryable<SysRole>()
                .Where(r => r.IsDeleted == false && r.Enabled)
                .OrderBy(r => r.Id)
                .ToListAsync();
            return ApiResult<List<SysRole>>.Ok(roles);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "列出角色");
            return ApiResult<List<SysRole>>.Fail("读取角色列表失败，详细信息已写入本地日志。");
        }
    }

    public async Task<ApiResult<RoleSaveModel>> GetAsync(long id)
    {
        try
        {
            if (AppHost.DemoMode)
            {
                var db = LocalDbStore.GetSnapshot();
                var role = db.Roles.FirstOrDefault(r => r.Id == id && !r.IsDeleted);
                if (role == null)
                    return ApiResult<RoleSaveModel>.Fail("角色不存在或已删除。");

                return ApiResult<RoleSaveModel>.Ok(new RoleSaveModel
                {
                    Id = role.Id,
                    RoleName = role.RoleName,
                    RoleCode = role.RoleCode,
                    Enabled = role.Enabled,
                    PermissionIds = db.RolePermissions.Where(rp => rp.RoleId == id).Select(rp => rp.PermissionId).ToList()
                });
            }

            var entity = await SqlSugarSetup.Db.Queryable<SysRole>().FirstAsync(r => r.Id == id && r.IsDeleted == false);
            if (entity == null)
                return ApiResult<RoleSaveModel>.Fail("角色不存在或已删除。");

            var permIds = await SqlSugarSetup.Db.Queryable<SysRolePermission>()
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return ApiResult<RoleSaveModel>.Ok(new RoleSaveModel
            {
                Id = entity.Id,
                RoleName = entity.RoleName,
                RoleCode = entity.RoleCode,
                Enabled = entity.Enabled,
                PermissionIds = permIds
            });
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "读取角色");
            return ApiResult<RoleSaveModel>.Fail("读取角色失败，详细信息已写入本地日志。");
        }
    }

    public async Task<ApiResult> SaveAsync(RoleSaveModel model)
    {
        try
        {
            model.RoleName = model.RoleName.Trim();
            model.RoleCode = model.RoleCode.Trim();
            if (model.RoleName.Length == 0 || model.RoleCode.Length == 0)
                return ApiResult.Fail("角色名称和编码不能为空。");

            if (AppHost.DemoMode)
            {
                return LocalDbStore.Update(db =>
                {
                    var dup = db.Roles.Any(r => !r.IsDeleted
                        && r.RoleCode.Equals(model.RoleCode, StringComparison.OrdinalIgnoreCase)
                        && r.Id != model.Id);
                    if (dup)
                        return ApiResult.Fail("角色编码已存在。");

                    SysRole role;
                    if (model.Id == 0)
                    {
                        role = new SysRole
                        {
                            Id = db.NextRoleId++,
                            RoleName = model.RoleName,
                            RoleCode = model.RoleCode,
                            Enabled = model.Enabled,
                            CreateTime = DateTime.Now,
                            CreateUserId = AppSession.UserId
                        };
                        db.Roles.Add(role);
                    }
                    else
                    {
                        role = db.Roles.FirstOrDefault(r => r.Id == model.Id && !r.IsDeleted)!;
                        if (role == null)
                            return ApiResult.Fail("角色不存在或已删除。");
                        role.RoleName = model.RoleName;
                        role.RoleCode = model.RoleCode;
                        role.Enabled = model.Enabled;
                        role.UpdateTime = DateTime.Now;
                        role.UpdateUserId = AppSession.UserId;
                    }

                    db.RolePermissions.RemoveAll(rp => rp.RoleId == role.Id);
                    foreach (var pid in model.PermissionIds.Distinct())
                    {
                        if (!db.Permissions.Any(p => p.Id == pid && !p.IsDeleted))
                            continue;
                        db.RolePermissions.Add(new SysRolePermission
                        {
                            Id = db.NextRolePermissionId++,
                            RoleId = role.Id,
                            PermissionId = pid
                        });
                    }

                    return ApiResult.Ok("保存成功。");
                });
            }

            var dbClient = SqlSugarSetup.Db;
            var exists = await dbClient.Queryable<SysRole>()
                .AnyAsync(r => r.IsDeleted == false && r.RoleCode == model.RoleCode && r.Id != model.Id);
            if (exists)
                return ApiResult.Fail("角色编码已存在。");

            await dbClient.Ado.BeginTranAsync();
            try
            {
                long roleId;
                if (model.Id == 0)
                {
                    var entity = new SysRole
                    {
                        RoleName = model.RoleName,
                        RoleCode = model.RoleCode,
                        Enabled = model.Enabled,
                        CreateTime = DateTime.Now,
                        CreateUserId = AppSession.UserId
                    };
                    roleId = await dbClient.Insertable(entity).ExecuteReturnBigIdentityAsync();
                }
                else
                {
                    var entity = await dbClient.Queryable<SysRole>().FirstAsync(r => r.Id == model.Id && r.IsDeleted == false);
                    if (entity == null)
                    {
                        await dbClient.Ado.RollbackTranAsync();
                        return ApiResult.Fail("角色不存在或已删除。");
                    }

                    entity.RoleName = model.RoleName;
                    entity.RoleCode = model.RoleCode;
                    entity.Enabled = model.Enabled;
                    entity.UpdateTime = DateTime.Now;
                    entity.UpdateUserId = AppSession.UserId;
                    await dbClient.Updateable(entity).ExecuteCommandAsync();
                    roleId = entity.Id;
                    await dbClient.Deleteable<SysRolePermission>().Where(rp => rp.RoleId == roleId).ExecuteCommandAsync();
                }

                if (model.PermissionIds.Count > 0)
                {
                    var links = model.PermissionIds.Distinct()
                        .Select(pid => new SysRolePermission { RoleId = roleId, PermissionId = pid })
                        .ToList();
                    await dbClient.Insertable(links).ExecuteCommandAsync();
                }

                await dbClient.Ado.CommitTranAsync();
                return ApiResult.Ok("保存成功。");
            }
            catch
            {
                await dbClient.Ado.RollbackTranAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "保存角色");
            return ApiResult.Fail("保存角色失败，详细信息已写入本地日志。");
        }
    }

    public async Task<ApiResult> DeleteAsync(long id)
    {
        try
        {
            if (AppHost.DemoMode)
            {
                return LocalDbStore.Update(db =>
                {
                    if (db.UserRoles.Any(ur => ur.RoleId == id))
                        return ApiResult.Fail("角色仍绑定用户，无法删除。");

                    var role = db.Roles.FirstOrDefault(r => r.Id == id && !r.IsDeleted);
                    if (role == null)
                        return ApiResult.Fail("角色不存在或已删除。");
                    role.IsDeleted = true;
                    role.UpdateTime = DateTime.Now;
                    db.RolePermissions.RemoveAll(rp => rp.RoleId == id);
                    return ApiResult.Ok("删除成功。");
                });
            }

            var used = await SqlSugarSetup.Db.Queryable<SysUserRole>().AnyAsync(ur => ur.RoleId == id);
            if (used)
                return ApiResult.Fail("角色仍绑定用户，无法删除。");

            var entity = await SqlSugarSetup.Db.Queryable<SysRole>().FirstAsync(r => r.Id == id && r.IsDeleted == false);
            if (entity == null)
                return ApiResult.Fail("角色不存在或已删除。");

            entity.IsDeleted = true;
            entity.UpdateTime = DateTime.Now;
            entity.UpdateUserId = AppSession.UserId;
            await SqlSugarSetup.Db.Updateable(entity).ExecuteCommandAsync();
            await SqlSugarSetup.Db.Deleteable<SysRolePermission>().Where(rp => rp.RoleId == id).ExecuteCommandAsync();
            return ApiResult.Ok("删除成功。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "删除角色");
            return ApiResult.Fail("删除角色失败，详细信息已写入本地日志。");
        }
    }
}
