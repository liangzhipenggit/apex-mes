namespace Mes.Common;

/// <summary>
/// 临时授权等操作同时写入本地日志和数据库，不写口令。
/// 数据库由启动时挂上的 <see cref="Persist"/> 完成；挂不上或写入失败时仍保留文件日志。
/// </summary>
public static class AuditLog
{
    private static readonly Lock Sync = new();

    public static Action<string>? Persist { get; set; }

    public static void Write(string message)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"audit-{DateTime.Now:yyyyMMdd}.log");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            lock (Sync)
                File.AppendAllText(path, line);
        }
        catch
        {
            // 文件失败不阻断现场操作。
        }

        try
        {
            Persist?.Invoke(message);
        }
        catch
        {
            // 数据库失败不阻断现场操作。
        }
    }
}
