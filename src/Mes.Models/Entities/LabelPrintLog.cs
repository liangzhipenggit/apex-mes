using SqlSugar;

namespace Mes.Models;

/// <summary>标签打印记录。字段与打印界面一致。</summary>
[SugarTable("Mes_LabelPrint")]
public class LabelPrintLog : BaseEntity
{
    [SugarColumn(Length = 64)]
    public string MaterialCode { get; set; } = string.Empty;

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? BatchNo { get; set; }

    [SugarColumn(Length = 128)]
    public string Barcode { get; set; } = string.Empty;

    public int Copies { get; set; } = 1;

    /// <summary>FastReport 或 BarTender。</summary>
    [SugarColumn(Length = 32)]
    public string Engine { get; set; } = string.Empty;

    public bool IsPreview { get; set; }

    [SugarColumn(Length = 260, IsNullable = true)]
    public string? TemplatePath { get; set; }

    [SugarColumn(Length = 128, IsNullable = true)]
    public string? PrinterName { get; set; }

    public bool Success { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Message { get; set; }
}
