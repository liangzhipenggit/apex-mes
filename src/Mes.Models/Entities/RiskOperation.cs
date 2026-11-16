using SqlSugar;

namespace Mes.Models;

/// <summary>强制过站、向 PLC 写入参数。执行前必须通过临时授权。</summary>
[SugarTable("Mes_RiskOperation")]
public class RiskOperation : BaseEntity
{
    [SugarColumn(Length = 64)]
    public string Operation { get; set; } = string.Empty;

    [SugarColumn(Length = 128, IsNullable = true)]
    public string? Barcode { get; set; }

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? OperatorName { get; set; }

    [SugarColumn(Length = 64)]
    public string AuthorizerName { get; set; } = string.Empty;

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? AuthorizerRealName { get; set; }

    public bool Success { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Message { get; set; }
}
