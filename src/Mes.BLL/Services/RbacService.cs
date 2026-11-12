using Mes.DAL;
using Mes.Models;

namespace Mes.BLL;

/// <summary>
/// 按用户汇总角色，再展开为菜单节点和按钮权限码。
/// </summary>
public class RbacService
{
    public async Task<(List<string> Codes, List<MenuNodeDto> Menus)> LoadForUserAsync(long userId)
    {
        var db = SqlSugarSetup.Db;
        var roleIds = await db.Queryable<SysUserRole>()
            .InnerJoin<SysRole>((ur, role) => ur.RoleId == role.Id)
            .Where((ur, role) => ur.UserId == userId && role.Enabled && role.IsDeleted == false)
            .Select((ur, role) => ur.RoleId)
            .ToListAsync();

        if (roleIds.Count == 0)
            return ([], []);

        var permissionIds = await db.Queryable<SysRolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        if (permissionIds.Count == 0)
            return ([], []);

        var granted = await db.Queryable<SysPermission>()
            .Where(p => permissionIds.Contains(p.Id) && p.IsDeleted == false)
            .OrderBy(p => p.Sort)
            .ToListAsync();

        var codes = granted
            .Select(p => p.PermCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var directories = await db.Queryable<SysPermission>()
            .Where(p => p.PermType == PermissionType.Directory && p.IsDeleted == false)
            .ToListAsync();

        var menus = MenuTreeBuilder.Build(granted.Concat(directories));
        return (codes, menus);
    }
}
