using SqlSugar;

namespace Mes.Models;

/// <summary>
/// 菜单与按钮权限。目录、菜单、按钮共用一张表，用 PermType 区分。
/// </summary>
[SugarTable("Sys_Permission")]
public class SysPermission : BaseEntity
{
    public long ParentId { get; set; }

    [SugarColumn(Length = 64)]
    public string PermName { get; set; } = string.Empty;

    [SugarColumn(Length = 64)]
    public string PermCode { get; set; } = string.Empty;

    public PermissionType PermType { get; set; }

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? FormKey { get; set; }

    public int Sort { get; set; }
}
