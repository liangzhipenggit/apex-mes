using SqlSugar;

namespace Mes.Models;

[SugarTable("Sys_Role")]
public class SysRole : BaseEntity
{
    [SugarColumn(Length = 64)]
    public string RoleName { get; set; } = string.Empty;

    [SugarColumn(Length = 64)]
    public string RoleCode { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}
