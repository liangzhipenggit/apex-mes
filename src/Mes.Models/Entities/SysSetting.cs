using SqlSugar;

namespace Mes.Models;

/// <summary>系统设置。键与界面一致，例如 Ui.FontFamily、Label.PrinterName、Scan.PortName。</summary>
[SugarTable("Sys_Setting")]
public class SysSetting : BaseEntity
{
    [SugarColumn(Length = 64)]
    public string SettingKey { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? SettingValue { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Remark { get; set; }
}
