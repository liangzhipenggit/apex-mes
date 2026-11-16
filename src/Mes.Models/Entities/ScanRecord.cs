using SqlSugar;

namespace Mes.Models;

/// <summary>过站扫码校验记录。</summary>
[SugarTable("Mes_ScanRecord")]
public class ScanRecord : BaseEntity
{
    [SugarColumn(Length = 128)]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>扫码框、手工、全局键盘、串口枪。</summary>
    [SugarColumn(Length = 32)]
    public string Source { get; set; } = string.Empty;

    [SugarColumn(Length = 32, IsNullable = true)]
    public string? PortName { get; set; }

    public bool Passed { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Message { get; set; }
}
