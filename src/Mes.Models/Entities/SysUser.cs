using SqlSugar;

namespace Mes.Models;

[SugarTable("Sys_User")]
public class SysUser : BaseEntity
{
    [SugarColumn(Length = 64)]
    public string UserName { get; set; } = string.Empty;

    [SugarColumn(Length = 128)]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(Length = 64)]
    public string RealName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}
