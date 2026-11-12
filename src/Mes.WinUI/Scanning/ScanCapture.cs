namespace Mes.WinUI;

/// <summary>
/// 绑定到指定输入框。扫码枪以键盘方式连发时，回车或短暂停顿都会提交；回车被吃掉，避免触发表单默认按钮。
/// </summary>
public static class ScanCapture
{
    private static readonly HashSet<Control> Bound = [];

    /// <summary>焦点正在已绑定的扫码框上时，全局钩子应让出按键。</summary>
    public static bool HasFocusedTarget()
    {
        foreach (var box in Bound)
        {
            if (!box.IsDisposed && box.Focused)
                return true;
        }

        return false;
    }

    public static void Bind(AntdUI.Input box, Func<string, Task> onCode, ScanCaptureOptions? options = null)
        => BindControl(box, () => box.Text, value => box.Text = value, box.Clear, onCode, options);

    public static void Bind(TextBox box, Func<string, Task> onCode, ScanCaptureOptions? options = null)
        => BindControl(box, () => box.Text, value => box.Text = value, box.Clear, onCode, options);

    private static void BindControl(
        Control box,
        Func<string> getText,
        Action<string> setText,
        Action clear,
        Func<string, Task> onCode,
        ScanCaptureOptions? options)
    {
        options ??= new ScanCaptureOptions();
        var debouncer = new ScanDebouncer();
        var timer = new System.Windows.Forms.Timer { Interval = Math.Max(20, options.IdleCommitMs) };
        long firstTick = 0;
        long lastTick = 0;
        var count = 0;
        var busy = 0;

        box.ImeMode = ImeMode.Disable;
        Bound.Add(box);
        box.Disposed += (_, _) => Bound.Remove(box);

        box.KeyPress += (_, e) =>
        {
            if (e.KeyChar is '\r' or '\n')
            {
                e.Handled = true;
                return;
            }

            var now = Environment.TickCount64;
            if (count == 0 || now - lastTick > options.MaxCharIntervalMs)
            {
                firstTick = now;
                count = 0;
            }

            lastTick = now;
            count++;
            timer.Stop();
            timer.Start();
        };

        box.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            e.Handled = true;
            timer.Stop();
            await CommitAsync(forceEnter: true);
        };

        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            if (IsRapid())
                await CommitAsync(forceEnter: false);
        };

        box.Disposed += (_, _) => timer.Dispose();

        async Task CommitAsync(bool forceEnter)
        {
            if (Interlocked.Exchange(ref busy, 1) == 1)
                return;

            try
            {
                var code = ScanText.Normalize(getText());
                var rapid = IsRapid();
                count = 0;
                if (code.Length < options.MinLength)
                    return;
                if (!rapid && !forceEnter)
                    return;
                if (!rapid && !options.CommitOnManualEnter)
                    return;
                if (debouncer.IsDuplicate(code, options.DebounceMs))
                {
                    if (options.ClearAfterRead)
                        clear();
                    return;
                }

                if (options.ClearAfterRead)
                    clear();
                else
                    setText(code);

                await onCode(code);
            }
            finally
            {
                Volatile.Write(ref busy, 0);
            }
        }

        bool IsRapid()
            => count >= options.MinLength
               && lastTick - firstTick <= (long)options.MaxCharIntervalMs * Math.Max(1, count - 1);
    }
}
