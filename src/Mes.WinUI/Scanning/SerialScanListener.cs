using System.IO.Ports;
using System.Text;

namespace Mes.WinUI;

/// <summary>串口扫码枪。累积到回车、换行，或连发后短暂停顿即提交。</summary>
public sealed class SerialScanListener : IDisposable
{
    private readonly SerialPort _port;
    private readonly ScanCaptureOptions _options;
    private readonly ScanDebouncer _debouncer = new();
    private readonly SynchronizationContext? _ui = SynchronizationContext.Current;
    private readonly StringBuilder _buffer = new();
    private readonly System.Threading.Timer _idle;
    private long _firstTick;
    private long _lastTick;
    private int _busy;

    public SerialScanListener(string portName, int baudRate = 9600, ScanCaptureOptions? options = null)
    {
        _options = options ?? new ScanCaptureOptions();
        _port = new SerialPort(portName, baudRate)
        {
            Encoding = Encoding.ASCII,
            DtrEnable = true,
            NewLine = "\r"
        };
        _idle = new System.Threading.Timer(_ => Publish(forceEnter: false), null, Timeout.Infinite, Timeout.Infinite);
        _port.DataReceived += OnData;
        _port.Open();
    }

    public event EventHandler<string>? Scanned;

    public void Dispose()
    {
        _idle.Dispose();
        _port.DataReceived -= OnData;
        if (_port.IsOpen)
            _port.Close();
        _port.Dispose();
    }

    private void OnData(object sender, SerialDataReceivedEventArgs e)
    {
        string chunk;
        try
        {
            chunk = _port.ReadExisting();
        }
        catch (Exception ex)
        {
            Mes.Common.ErrorLog.Write(ex, "读取串口扫码枪");
            return;
        }

        lock (_buffer)
        {
            foreach (var ch in chunk)
            {
                if (ch is '\r' or '\n' or '\u0002' or '\u0003')
                {
                    Publish(forceEnter: true);
                    continue;
                }

                var now = Environment.TickCount64;
                if (_buffer.Length == 0 || now - _lastTick > _options.MaxCharIntervalMs)
                    _firstTick = now;

                _lastTick = now;
                _buffer.Append(ch);
                _idle.Change(_options.IdleCommitMs, Timeout.Infinite);
            }
        }
    }

    private void Publish(bool forceEnter)
    {
        string code;
        lock (_buffer)
        {
            code = ScanText.Normalize(_buffer.ToString());
            var rapid = _buffer.Length >= _options.MinLength
                        && _lastTick - _firstTick <= (long)_options.MaxCharIntervalMs * Math.Max(1, _buffer.Length - 1);
            if (!forceEnter && !rapid)
                return;

            _buffer.Clear();
        }

        if (code.Length < _options.MinLength)
            return;
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            return;

        try
        {
            if (_debouncer.IsDuplicate(code, _options.DebounceMs))
                return;

            if (_ui != null)
                _ui.Post(_ => Scanned?.Invoke(this, code), null);
            else
                Scanned?.Invoke(this, code);
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
        }
    }
}
