namespace Mes.WinUI;

public class ElevationForm : AntdUI.Window
{
    private readonly AntdUI.Input _account = new() { PlaceholderText = "授权账号", PrefixSvg = "UserOutlined" };
    private readonly AntdUI.Input _password = new()
    {
        PlaceholderText = "密码",
        UseSystemPasswordChar = true,
        PrefixSvg = "LockOutlined"
    };

    public ElevationForm(string? operation = null)
    {
        // operation 仅供调用方语义保留，授权窗不再展示具体操作名
        _ = operation;
        Text = "临时授权";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 360);
        Resizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.PageBack;

        var header = UiChrome.CreateDialogHeader("临时授权", "工程师 / 管理员二次确认");

        var body = new AntdUI.Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiTheme.SpaceXl),
            Back = UiTheme.CardBack,
            BorderWidth = 0,
            Radius = UiTheme.CardRadius
        };

        var hint = new AntdUI.Alert
        {
            Text = "该操作需要工程师或管理员授权。当前登录身份不会被切换。",
            Icon = AntdUI.TType.Warn,
            CloseIcon = false,
            Dock = DockStyle.Top,
            Height = 64,
            Margin = new Padding(0, 0, 0, UiTheme.SpaceMd)
        };

        var ok = new AntdUI.Button
        {
            Text = "授权并执行",
            Type = AntdUI.TTypeMini.Primary,
            Size = new Size(140, UiTheme.ControlHeight)
        };
        PageToolbar.StyleIcon(ok, MenuIcons.Action.Check);
        var cancel = new AntdUI.Button
        {
            Text = "取消",
            Type = AntdUI.TTypeMini.Default,
            Size = new Size(96, UiTheme.ControlHeight)
        };
        ok.Click += (_, _) =>
        {
            if (_account.Text.Trim().Length == 0 || _password.Text.Length == 0)
            {
                UiNotify.Warn(this, "请输入授权账号和密码。");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        body.Controls.AddRange(hint, _account, _password, ok, cancel);
        body.Layout += (_, _) =>
        {
            var top = hint.Bottom + UiTheme.SpaceLg;
            _account.SetBounds(0, top, body.ClientSize.Width, UiTheme.ControlHeight);
            _password.SetBounds(0, top + 52, body.ClientSize.Width, UiTheme.ControlHeight);
            ok.SetBounds(body.ClientSize.Width - 240, top + 112, 132, UiTheme.ControlHeight);
            cancel.SetBounds(body.ClientSize.Width - 96, top + 112, 96, UiTheme.ControlHeight);
        };

        Controls.Add(body);
        Controls.Add(header);
    }

    public string Account => _account.Text.Trim();

    public string Password => _password.Text;
}
