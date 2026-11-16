using Mes.BLL;

namespace Mes.WinUI;

/// <summary>
/// 过站扫码与危险操作示例。PLC 写入、强制过站只有临时授权通过后才会进入方法体。
/// </summary>
public class StationGateForm : Form
{
    private readonly AntdUI.Input _scan = new() { PlaceholderText = "扫码或手工输入后回车", AllowClear = true };
    private readonly AntdUI.Input _portName = new() { Text = "COM3" };
    private readonly AntdUI.Alert _result = new()
    {
        Icon = AntdUI.TType.Info,
        CloseIcon = false,
        TextTitle = "校验结果"
    };
    private readonly GlobalScanListener _keyboard;
    private SerialScanListener? _serial;

    public StationGateForm()
    {
        Text = "过站防错";
        BackColor = UiTheme.PageBack;

        var root = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiTheme.SpaceLg),
            BackColor = UiTheme.PageBack
        };

        var scanCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 180,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceLg, UiTheme.SpaceXl, UiTheme.SpaceLg),
            Margin = new Padding(0, 0, 0, UiTheme.SpaceMd)
        };

        var y = 0;
        FormUi.AddSection(scanCard, "扫码校验", ref y);
        var scanLabel = FormUi.Caption("条码", 0, 0, 64);
        scanLabel.TextAlign = ContentAlignment.MiddleLeft;
        FormUi.Place(scanCard, scanLabel, 0, y, 64, UiTheme.ControlHeight);
        FormUi.Place(scanCard, _scan, 72, y, 360, UiTheme.ControlHeight);
        ScanCapture.Bind(_scan, code =>
        {
            AcceptCode(code, "扫码框");
            return Task.CompletedTask;
        }, new ScanCaptureOptions { ClearAfterRead = false });

        var check = new AntdUI.Button
        {
            Text = "校验",
            Type = AntdUI.TTypeMini.Primary,
            Size = new Size(108, UiTheme.ControlHeight)
        };
        PageToolbar.StyleIcon(check, MenuIcons.Action.Check);
        check.Click += (_, _) => AcceptCode(ScanText.Normalize(_scan.Text), "手工");
        FormUi.Place(scanCard, check, 448, y, 108, UiTheme.ControlHeight);
        y += FormUi.RowHeight;

        var portLabel = FormUi.Caption("串口", 0, 0, 64);
        portLabel.TextAlign = ContentAlignment.MiddleLeft;
        FormUi.Place(scanCard, portLabel, 0, y, 64, UiTheme.ControlHeight);
        FormUi.Place(scanCard, _portName, 72, y, 140, UiTheme.ControlHeight);

        var open = new AntdUI.Button
        {
            Text = "打开串口枪",
            Type = AntdUI.TTypeMini.Default,
            Size = new Size(128, UiTheme.ControlHeight)
        };
        var close = new AntdUI.Button
        {
            Text = "关闭",
            Type = AntdUI.TTypeMini.Default,
            Size = new Size(88, UiTheme.ControlHeight)
        };
        open.Click += (_, _) => OpenSerial();
        close.Click += (_, _) => CloseSerial();
        FormUi.Place(scanCard, open, 228, y, 128, UiTheme.ControlHeight);
        FormUi.Place(scanCard, close, 368, y, 88, UiTheme.ControlHeight);

        var actionCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceLg, UiTheme.SpaceXl, UiTheme.SpaceLg),
            Margin = new Padding(0, 0, 0, UiTheme.SpaceMd)
        };
        var ay = 0;
        FormUi.AddSection(actionCard, "危险操作（需临时授权）", ref ay);
        var plc = new AntdUI.Button
        {
            Text = "向PLC写入参数",
            Type = AntdUI.TTypeMini.Warn,
            Size = new Size(176, 44)
        };
        PageToolbar.StyleIcon(plc, MenuIcons.Action.Warn);
        var pass = new AntdUI.Button
        {
            Text = "强制过站",
            Type = AntdUI.TTypeMini.Error,
            Size = new Size(128, 44)
        };
        plc.Click += async (_, _) => await WritePlcAsync();
        pass.Click += async (_, _) => await ForcePassAsync();
        FormUi.Place(actionCard, plc, 0, ay, 176, 44);
        FormUi.Place(actionCard, pass, 192, ay, 128, 44);

        var resultCard = UiChrome.CreateCard(DockStyle.Fill, shadow: 4);
        resultCard.Margin = new Padding(0);
        _result.Dock = DockStyle.Fill;
        _result.Text = "USB 扫码枪可在任意焦点下连发，回车或短暂停顿后自动校验。演示授权账号：engineer 或 admin，密码 123456。操作员账号不能授权。";
        resultCard.Controls.Add(_result);

        _keyboard = new GlobalScanListener(new ScanCaptureOptions { CommitOnManualEnter = false });
        _keyboard.Scanned += (_, code) => AcceptCode(code, "全局键盘");
        _keyboard.Start();

        root.Controls.Add(resultCard);
        root.Controls.Add(actionCard);
        root.Controls.Add(scanCard);
        Controls.Add(root);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keyboard.Dispose();
            CloseSerial();
        }

        base.Dispose(disposing);
    }

    private void AcceptCode(string code, string source)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;

        _scan.Text = code;
        _result.Icon = AntdUI.TType.Success;
        _result.TextTitle = "校验通过";
        _result.Text = $"{DateTime.Now:HH:mm:ss}  {source}校验通过：{code}";
        DbActivityLog.Scan(
            code,
            source,
            source == "串口枪" ? _portName.Text.Trim() : null,
            true,
            _result.Text);
    }

    private void OpenSerial()
    {
        try
        {
            CloseSerial();
            _serial = new SerialScanListener(_portName.Text.Trim());
            _serial.Scanned += (_, code) => AcceptCode(code, "串口枪");
            _result.Icon = AntdUI.TType.Info;
            _result.TextTitle = "串口";
            _result.Text = $"已打开 {_portName.Text.Trim()}";
        }
        catch (Exception ex)
        {
            Mes.Common.ErrorLog.Write(ex, "打开串口扫码枪");
            UiNotify.Warn(FindForm(), "打开串口失败：" + ex.Message);
        }
    }

    private void CloseSerial()
    {
        _serial?.Dispose();
        _serial = null;
    }

    private async Task WritePlcAsync()
    {
        var code = ScanText.Normalize(_scan.Text);
        if (code.Length < 4)
        {
            UiNotify.Warn(FindForm(), "请先扫描或输入有效条码。");
            return;
        }

        await OperationGuard.RunAsync(FindForm(), "向PLC写入参数", () =>
        {
            _result.Icon = AntdUI.TType.Success;
            _result.TextTitle = "PLC 写入";
            _result.Text = $"已授权写入参数（演示）：{code}";
            UiNotify.Success(FindForm(), "PLC 参数写入成功（演示）。");
            return Task.CompletedTask;
        }, barcode: code);
    }

    private async Task ForcePassAsync()
    {
        await OperationGuard.RunAsync(FindForm(), "强制过站", () =>
        {
            _result.Icon = AntdUI.TType.Warn;
            _result.TextTitle = "强制过站";
            _result.Text = "已授权执行强制过站（演示）。";
            UiNotify.Success(FindForm(), "强制过站已执行（演示）。");
            return Task.CompletedTask;
        }, barcode: ScanText.Normalize(_scan.Text));
    }
}
