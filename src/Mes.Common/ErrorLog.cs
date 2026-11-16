namespace Mes.Common;

/// <summary>
/// 异常同时写入本地日志和数据库。
/// 数据库由启动时挂上的 <see cref="Persist"/> 完成；挂不上或写入失败时仍保留文件日志。
/// </summary>
public static class ErrorLog
{
    private static readonly Lock Sync = new();

    public static Action<Exception, string>? Persist { get; set; }

    public static void Write(Exception exception, string action)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"error-{DateTime.Now:yyyyMMdd}.log");
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {action}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}";
            lock (Sync)
                File.AppendAllText(path, text);
        }
        catch
        {
            // 文件失败不再向外抛。
        }

        try
        {
            Persist?.Invoke(exception, action);
        }
        catch
        {
            // 数据库失败不再向外抛。
        }
    }
}
