namespace Mes.WinUI;

public sealed class ScanCaptureOptions
{
    /// <summary>相邻两个字符超过该间隔，就视为人工输入而不是扫码枪连发。</summary>
    public int MaxCharIntervalMs { get; set; } = 50;

    /// <summary>连发结束后静默该时长即提交，兼容不带回车后缀的扫码枪。</summary>
    public int IdleCommitMs { get; set; } = 80;

    /// <summary>同一条码在该时间内重复出现时丢弃。</summary>
    public int DebounceMs { get; set; } = 400;

    public int MinLength { get; set; } = 4;

    /// <summary>为 false 时保留文本框内容，供查询条件继续使用。</summary>
    public bool ClearAfterRead { get; set; } = true;

    /// <summary>人工慢速输入后按回车也提交。全局钩子应关闭，避免抢走普通回车。</summary>
    public bool CommitOnManualEnter { get; set; } = true;
}

public static class ScanText
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var chars = raw.Where(ch => ch is not ('\r' or '\n' or '\u0002' or '\u0003' or '\u001d')).ToArray();
        return new string(chars).Trim();
    }
}

/// <summary>同一条码短时间重复触发时只保留第一次。</summary>
public sealed class ScanDebouncer
{
    private string _last = string.Empty;
    private long _at;

    public bool IsDuplicate(string code, int debounceMs)
    {
        var now = Environment.TickCount64;
        if (string.Equals(code, _last, StringComparison.OrdinalIgnoreCase) && now - _at < debounceMs)
            return true;

        _last = code;
        _at = now;
        return false;
    }
}
