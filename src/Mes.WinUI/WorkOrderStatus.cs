namespace Mes.Models;

public static class WorkOrderStatus
{
    public const int Draft = 0;
    public const int Released = 1;
    public const int Closed = 2;

    public static string NameOf(int status) => status switch
    {
        Draft => "草稿",
        Released => "已下达",
        Closed => "已关闭",
        _ => "未知"
    };
}
