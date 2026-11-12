using Mes.DAL;
using Mes.Models;

namespace Mes.BLL;

internal static class RbacHelper
{
    public static (List<string> Codes, List<MenuNodeDto> Menus) LoadFromLocal(long userId)
    {
        var db = LocalDbStore.GetSnapshot();
        var roleIds = db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles.Where(r => r.Enabled && !r.IsDeleted), ur => ur.RoleId, r => r.Id, (ur, _) => ur.RoleId)
            .Distinct()
            .ToList();

        if (roleIds.Count == 0)
            return ([], []);

        var permissionIds = db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToHashSet();

        var granted = db.Permissions
            .Where(p => !p.IsDeleted && permissionIds.Contains(p.Id))
            .OrderBy(p => p.Sort)
            .ToList();

        var codes = granted
            .Select(p => p.PermCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var directories = db.Permissions
            .Where(p => !p.IsDeleted && p.PermType == PermissionType.Directory)
            .ToList();

        return (codes, MenuTreeBuilder.Build(granted.Concat(directories)));
    }
}
