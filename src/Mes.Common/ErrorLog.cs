namespace Mes.Common;

/// <summary>
/// 把异常追加到程序目录下的 logs/error-yyyyMMdd.log。
/// </summary>
public static class ErrorLog
{
    private static readonly Lock Sync = new();

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
            // 日志本身失败时不再向外抛。
        }
    }
}
