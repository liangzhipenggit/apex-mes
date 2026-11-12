namespace Mes.Common;

/// <summary>
/// 追加到程序目录 logs/audit-yyyyMMdd.log，记录临时授权等操作，不写口令。
/// </summary>
public static class AuditLog
{
    private static readonly Lock Sync = new();

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
            // 审计失败不阻断现场操作。
        }
    }
}
