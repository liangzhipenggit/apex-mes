using SqlSugar;

namespace Mes.Models;

[SugarTable("Sys_RolePermission")]
public class SysRolePermission
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    public long RoleId { get; set; }

    public long PermissionId { get; set; }
}
