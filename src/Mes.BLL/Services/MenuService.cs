using Mes.Common;
using Mes.DAL;
using Mes.Models;

namespace Mes.BLL;

public class MenuService
{
    public async Task<ApiResult<List<SysPermission>>> QueryAsync(MenuQuery query)
    {
        try
        {
            var keyword = query.Keyword?.Trim();
            if (AppHost.DemoMode)
            {
                var db = LocalDbStore.GetSnapshot();
                IEnumerable<SysPermission> rows = db.Permissions.Where(p => !p.IsDeleted);
                if (!string.IsNullOrEmpty(keyword))
                {
                    rows = rows.Where(p =>
                        p.PermName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || p.PermCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || (p.FormKey?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                if (query.PermType.HasValue)
                    rows = rows.Where(p => p.PermType == query.PermType.Value);

                var list = rows.OrderBy(p => p.Sort).ThenBy(p => p.Id).ToList();
                return ApiResult<List<SysPermission>>.Ok(list);
            }

            var q = SqlSugarSetup.Db.Queryable<SysPermission>().Where(p => p.IsDeleted == false);
            if (!string.IsNullOrEmpty(keyword))
            {
                q = q.Where(p => p.PermName.Contains(keyword)
                                 || p.PermCode.Contains(keyword)
                                 || (p.FormKey != null && p.FormKey.Contains(keyword)));
            }

            if (query.PermType.HasValue)
                q = q.Where(p => p.PermType == query.PermType.Value);

            var items = await q.OrderBy(p => p.Sort).OrderBy(p => p.Id).ToListAsync();
            return ApiResult<List<SysPermission>>.Ok(items);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "查询菜单");
            return ApiResult<List<SysPermission>>.Fail("查询菜单失败，详细信息已写入日志。");
        }
    }

    public async Task<ApiResult<SysPermission>> GetAsync(long id)
    {
        try
        {
            if (AppHost.DemoMode)
            {
                var db = LocalDbStore.GetSnapshot();
                var item = db.Permissions.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                return item == null
                    ? ApiResult<SysPermission>.Fail("菜单不存在或已删除。")
                    : ApiResult<SysPermission>.Ok(Clone(item));
            }

            var entity = await SqlSugarSetup.Db.Queryable<SysPermission>()
                .FirstAsync(p => p.Id == id && p.IsDeleted == false);
            return entity == null
                ? ApiResult<SysPermission>.Fail("菜单不存在或已删除。")
                : ApiResult<SysPermission>.Ok(entity);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "读取菜单");
            return ApiResult<SysPermission>.Fail("读取菜单失败，详细信息已写入日志。");
        }
    }

    public async Task<ApiResult> SaveAsync(MenuSaveModel model)
    {
        try
        {
            var error = Validate(model);
            if (error != null)
                return ApiResult.Fail(error);

            model.PermName = model.PermName.Trim();
            model.PermCode = model.PermCode.Trim();
            model.FormKey = string.IsNullOrWhiteSpace(model.FormKey) ? null : model.FormKey.Trim();

            if (AppHost.DemoMode)
            {
                return LocalDbStore.Update(db =>
                {
                    var dup = db.Permissions.Any(p => !p.IsDeleted
                        && p.PermCode.Equals(model.PermCode, StringComparison.OrdinalIgnoreCase)
                        && p.Id != model.Id);
                    if (dup)
                        return ApiResult.Fail("权限编码已存在。");

                    if (model.ParentId > 0 && !db.Permissions.Any(p => p.Id == model.ParentId && !p.IsDeleted))
                        return ApiResult.Fail("上级节点不存在。");

                    if (model.Id > 0 && model.ParentId == model.Id)
                        return ApiResult.Fail("上级节点不能是自身。");

                    if (model.Id == 0)
                    {
                        var entity = new SysPermission
                        {
                            Id = db.NextPermissionId++,
                            ParentId = model.ParentId,
                            PermName = model.PermName,
                            PermCode = model.PermCode,
                            PermType = model.PermType,
                            FormKey = model.FormKey,
                            Sort = model.Sort,
                            CreateTime = DateTime.Now,
                            CreateUserId = AppSession.UserId
                        };
                        db.Permissions.Add(entity);

                        // 新菜单默认授权给管理员角色
                        if (db.Roles.Any(r => r.Id == 1 && !r.IsDeleted))
                        {
                            db.RolePermissions.Add(new SysRolePermission
                            {
                                Id = db.NextRolePermissionId++,
                                RoleId = 1,
                                PermissionId = entity.Id
                            });
                        }
                    }
                    else
                    {
                        var entity = db.Permissions.FirstOrDefault(p => p.Id == model.Id && !p.IsDeleted);
                        if (entity == null)
                            return ApiResult.Fail("菜单不存在或已删除。");
                        if (IsDescendant(db, model.Id, model.ParentId))
                            return ApiResult.Fail("上级节点不能是当前节点的子级。");

                        entity.ParentId = model.ParentId;
                        entity.PermName = model.PermName;
                        entity.PermCode = model.PermCode;
                        entity.PermType = model.PermType;
                        entity.FormKey = model.FormKey;
                        entity.Sort = model.Sort;
                        entity.UpdateTime = DateTime.Now;
                        entity.UpdateUserId = AppSession.UserId;
                    }

                    return ApiResult.Ok("保存成功。");
                });
            }

            var dbClient = SqlSugarSetup.Db;
            var exists = await dbClient.Queryable<SysPermission>()
                .AnyAsync(p => p.IsDeleted == false && p.PermCode == model.PermCode && p.Id != model.Id);
            if (exists)
                return ApiResult.Fail("权限编码已存在。");

            if (model.ParentId > 0)
            {
                var parentOk = await dbClient.Queryable<SysPermission>()
                    .AnyAsync(p => p.Id == model.ParentId && p.IsDeleted == false);
                if (!parentOk)
                    return ApiResult.Fail("上级节点不存在。");
            }

            if (model.Id == 0)
            {
                var entity = new SysPermission
                {
                    ParentId = model.ParentId,
                    PermName = model.PermName,
                    PermCode = model.PermCode,
                    PermType = model.PermType,
                    FormKey = model.FormKey,
                    Sort = model.Sort,
                    CreateTime = DateTime.Now,
                    CreateUserId = AppSession.UserId
                };
                var newId = await dbClient.Insertable(entity).ExecuteReturnBigIdentityAsync();
                await dbClient.Insertable(new SysRolePermission { RoleId = 1, PermissionId = newId }).ExecuteCommandAsync();
            }
            else
            {
                var entity = await dbClient.Queryable<SysPermission>()
                    .FirstAsync(p => p.Id == model.Id && p.IsDeleted == false);
                if (entity == null)
                    return ApiResult.Fail("菜单不存在或已删除。");

                entity.ParentId = model.ParentId;
                entity.PermName = model.PermName;
                entity.PermCode = model.PermCode;
                entity.PermType = model.PermType;
                entity.FormKey = model.FormKey;
                entity.Sort = model.Sort;
                entity.UpdateTime = DateTime.Now;
                entity.UpdateUserId = AppSession.UserId;
                await dbClient.Updateable(entity).ExecuteCommandAsync();
            }

            return ApiResult.Ok("保存成功。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "保存菜单");
            return ApiResult.Fail("保存菜单失败，详细信息已写入日志。");
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
                    if (db.Permissions.Any(p => !p.IsDeleted && p.ParentId == id))
                        return ApiResult.Fail("请先删除子节点。");

                    var item = db.Permissions.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                    if (item == null)
                        return ApiResult.Fail("菜单不存在或已删除。");

                    item.IsDeleted = true;
                    item.UpdateTime = DateTime.Now;
                    db.RolePermissions.RemoveAll(rp => rp.PermissionId == id);
                    return ApiResult.Ok("删除成功。");
                });
            }

            var hasChild = await SqlSugarSetup.Db.Queryable<SysPermission>()
                .AnyAsync(p => p.ParentId == id && p.IsDeleted == false);
            if (hasChild)
                return ApiResult.Fail("请先删除子节点。");

            var entity = await SqlSugarSetup.Db.Queryable<SysPermission>()
                .FirstAsync(p => p.Id == id && p.IsDeleted == false);
            if (entity == null)
                return ApiResult.Fail("菜单不存在或已删除。");

            entity.IsDeleted = true;
            entity.UpdateTime = DateTime.Now;
            entity.UpdateUserId = AppSession.UserId;
            await SqlSugarSetup.Db.Updateable(entity).ExecuteCommandAsync();
            await SqlSugarSetup.Db.Deleteable<SysRolePermission>().Where(rp => rp.PermissionId == id).ExecuteCommandAsync();
            return ApiResult.Ok("删除成功。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "删除菜单");
            return ApiResult.Fail("删除菜单失败，详细信息已写入日志。");
        }
    }

    public static string? Validate(MenuSaveModel model)
    {
        if (string.IsNullOrWhiteSpace(model.PermName))
            return "名称不能为空。";
        if (string.IsNullOrWhiteSpace(model.PermCode))
            return "权限编码不能为空。";
        if (model.PermType == PermissionType.Menu && string.IsNullOrWhiteSpace(model.FormKey))
            return "菜单类型必须填写 FormKey。";
        return null;
    }

    private static bool IsDescendant(LocalDbSnapshot db, long nodeId, long candidateParentId)
    {
        var current = candidateParentId;
        var guard = 0;
        while (current > 0 && guard++ < 64)
        {
            if (current == nodeId)
                return true;
            current = db.Permissions.FirstOrDefault(p => p.Id == current)?.ParentId ?? 0;
        }

        return false;
    }

    private static SysPermission Clone(SysPermission p) => new()
    {
        Id = p.Id,
        ParentId = p.ParentId,
        PermName = p.PermName,
        PermCode = p.PermCode,
        PermType = p.PermType,
        FormKey = p.FormKey,
        Sort = p.Sort,
        CreateTime = p.CreateTime,
        CreateUserId = p.CreateUserId,
        UpdateTime = p.UpdateTime,
        UpdateUserId = p.UpdateUserId,
        IsDeleted = p.IsDeleted
    };
}
