using System.Text;

namespace Mes.WinUI;

/// <summary>
/// 应用级键盘钩子，用于 USB 扫码枪模拟键盘。只在字符间隔足够短时认定为扫码，并吞掉结尾回车。
/// </summary>
public sealed class GlobalScanListener : IMessageFilter, IDisposable
{
    private const int WmChar = 0x0102;

    private readonly ScanCaptureOptions _options;
    private readonly ScanDebouncer _debouncer = new();
    private readonly System.Threading.Timer _idle;
    private readonly SynchronizationContext? _ui = SynchronizationContext.Current;
    private readonly StringBuilder _buffer = new();
    private long _firstTick;
    private long _lastTick;
    private long _swallowNewlineUntil;
    private int _busy;

    public GlobalScanListener(ScanCaptureOptions? options = null)
    {
        _options = options ?? new ScanCaptureOptions { CommitOnManualEnter = false, ClearAfterRead = false };
        _idle = new System.Threading.Timer(_ =>
        {
            if (_ui != null)
                _ui.Post(_ => Commit(forceEnter: false), null);
            else
                Commit(forceEnter: false);
        }, null, Timeout.Infinite, Timeout.Infinite);
    }

    public event EventHandler<string>? Scanned;

    public void Start() => Application.AddMessageFilter(this);

    public bool PreFilterMessage(ref Message message)
    {
        if (ScanCapture.HasFocusedTarget())
            return false;

        if (message.Msg == 0x0100)
        {
            var key = (Keys)(int)message.WParam & Keys.KeyCode;
            if (key != Keys.Enter || !IsRapid())
                return false;

            _swallowNewlineUntil = Environment.TickCount64 + 30;
            Commit(forceEnter: true);
            return true;
        }

        if (message.Msg != WmChar)
            return false;

        var ch = (char)message.WParam;
        var now = Environment.TickCount64;
        if (ch is '\r' or '\n' && now <= _swallowNewlineUntil)
            return true;

        if (ch is '\r' or '\n')
        {
            var rapid = IsRapid();
            if (!rapid && !_options.CommitOnManualEnter)
            {
                _buffer.Clear();
                return false;
            }

            _swallowNewlineUntil = now + 30;
            Commit(forceEnter: true);
            return rapid;
        }

        if (ch < ' ')
            return false;

        if (_buffer.Length == 0 || now - _lastTick > _options.MaxCharIntervalMs)
        {
            _buffer.Clear();
            _firstTick = now;
        }

        var continuing = _buffer.Length > 0;

        _lastTick = now;
        _buffer.Append(ch);
        _idle.Change(_options.IdleCommitMs, Timeout.Infinite);
        return continuing && IsRapid();
    }

    public void Dispose()
    {
        Application.RemoveMessageFilter(this);
        _idle.Dispose();
    }

    private void Commit(bool forceEnter)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            return;

        try
        {
            var code = ScanText.Normalize(_buffer.ToString());
            var rapid = IsRapid();
            _buffer.Clear();
            if (code.Length < _options.MinLength)
                return;
            if (!rapid && !forceEnter)
                return;
            if (!rapid && !_options.CommitOnManualEnter)
                return;
            if (_debouncer.IsDuplicate(code, _options.DebounceMs))
                return;

            Scanned?.Invoke(this, code);
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
        }
    }

    private bool IsRapid()
        => _buffer.Length >= _options.MinLength
           && _lastTick - _firstTick <= (long)_options.MaxCharIntervalMs * Math.Max(1, _buffer.Length - 1);
}
