namespace Mes.Models;

public class WorkOrderQuery
{
    public string? OrderNo { get; set; }

    public int? Status { get; set; }

    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
