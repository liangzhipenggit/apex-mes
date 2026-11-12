using SqlSugar;

namespace Mes.Models;

[SugarTable("Mes_WorkOrder")]
public class WorkOrder : BaseEntity
{
    [SugarColumn(Length = 32)]
    public string OrderNo { get; set; } = string.Empty;

    [SugarColumn(Length = 64)]
    public string ProductCode { get; set; } = string.Empty;

    [SugarColumn(Length = 128)]
    public string ProductName { get; set; } = string.Empty;

    [SugarColumn(Length = 18, DecimalDigits = 2)]
    public decimal PlanQty { get; set; }

    public int Status { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? PlanStart { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? PlanEnd { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Remark { get; set; }
}
