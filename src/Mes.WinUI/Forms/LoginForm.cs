using Mes.BLL;
using Mes.Common;

namespace Mes.WinUI;

public class LoginForm : AntdUI.Window
{
    private readonly AntdUI.Input _user = new()
    {
        PlaceholderText = "用户名",
        PrefixSvg = "UserOutlined"
    };
    private readonly AntdUI.Input _password = new()
    {
        PlaceholderText = "密码",
        UseSystemPasswordChar = true,
        PrefixSvg = "LockOutlined"
    };
    private readonly AntdUI.Button _login = new()
    {
        Text = "登 录",
        Type = AntdUI.TTypeMini.Primary,
        IconSvg = MenuIcons.Action.Login
    };
    private bool _loggingIn;

    public LoginForm()
    {
        Text = $"{AppBrand.FullName} 登录";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 620);
        MinimumSize = new Size(640, 620);
        Resizable = false;
        MaximizeBox = false;
        BackColor = UiTheme.PageBack;
        BorderColor = Color.Transparent;

        var header = new AntdUI.PageHeader
        {
            Text = AppBrand.FullName,
            SubText = AppBrand.Tagline,
            Dock = DockStyle.Top,
            Height = UiTheme.HeaderHeight,
            ShowButton = true,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = UiTheme.DialogHeaderBack,
            ForeColor = Color.White,
            DividerShow = false
        };

        // 无大块 Shadow，避免背后出现灰色方框
        var card = new AntdUI.Panel
        {
            Shadow = 0,
            Radius = 14,
            BorderWidth = 0,
            BorderColor = Color.Transparent,
            Back = UiTheme.CardBack,
            Padding = new Padding(36, 32, 36, 32)
        };

        var brand = new Label
        {
            Text = AppBrand.Name,
            AutoSize = false,
            Font = UiTheme.ShellFont(26F, FontStyle.Bold),
            ForeColor = UiTheme.Primary,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        var subtitle = new Label
        {
            Text = "Advanced Manufacturing Execution System",
            AutoSize = false,
            Font = UiTheme.ShellFont(9F),
            ForeColor = UiTheme.MutedText,
            TextAlign = ContentAlignment.TopRight,
            BackColor = Color.Transparent
        };
        var title = new Label
        {
            Text = "账号登录",
            AutoSize = false,
            Font = UiTheme.ShellFont(UiTheme.ShellFontSizeTitle, FontStyle.Bold),
            ForeColor = UiTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        PageToolbar.StyleIcon(_login);
        _login.Font = UiTheme.ShellFont(UiTheme.ShellFontSizeLg, FontStyle.Bold);
        _user.Font = UiTheme.ShellFont();
        _password.Font = UiTheme.ShellFont();

        card.Controls.AddRange(brand, subtitle, title, _user, _password, _login);
        card.Resize += (_, _) => LayoutFields(card, brand, subtitle, title);

        var body = new TechBackPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(40, 36, 40, 40),
            ClearColor = Color.FromArgb(230, 236, 244),
            BackdropOpacity = 0.55f,
            WashColor = Color.FromArgb(100, 236, 242, 248),
            BackColor = Color.FromArgb(230, 236, 244)
        };
        body.Controls.Add(card);
        body.Resize += (_, _) => LayoutCard(body, card);

        Controls.Add(body);
        Controls.Add(header);

        _login.Click += async (_, _) => await LoginAsync();
        _user.KeyDown += OnEnterKey;
        _password.KeyDown += OnEnterKey;
        Shown += (_, _) =>
        {
            LayoutCard(body, card);
            LayoutFields(card, brand, subtitle, title);
            _user.Focus();
        };
    }

    private void LayoutFields(Control card, Label brand, Label subtitle, Label title)
    {
        var content = card.DisplayRectangle;
        var x = content.X;
        var w = Math.Max(200, content.Width);
        var y = content.Y;

        brand.AutoSize = false;
        brand.TextAlign = ContentAlignment.MiddleCenter;
        var brandSize = TextSize(brand.Text, brand.Font);
        var brandW = brandSize.Width + 16;
        var brandX = x + Math.Max(0, (w - brandW) / 2);
        var brandH = brandSize.Height;
        brand.SetBounds(brandX, y, brandW, brandH);

        // 副标题居中，并紧贴主标题，纵向间隔为 0
        y += brandH;
        FitSubtitle(subtitle, w);
        subtitle.AutoSize = false;
        subtitle.TextAlign = ContentAlignment.MiddleCenter;
        var subSize = TextSize(subtitle.Text, subtitle.Font);
        var subW = subSize.Width + 16;
        var subX = x + Math.Max(0, (w - subW) / 2);
        subtitle.SetBounds(subX, y, subW, subSize.Height);

        y += subtitle.Height + 12;
        title.TextAlign = ContentAlignment.MiddleCenter;
        title.SetBounds(x, y, w, 36);
        y += 48;

        _user.SetBounds(x, y, w, UiTheme.ControlHeight);
        y += 56;

        _password.SetBounds(x, y, w, UiTheme.ControlHeight);
        y += 64;

        _login.SetBounds(x, y, w, 48);
    }

    /// <summary>只在超出卡片可用宽度时缩小字号，避免把 System 挤到下一行。</summary>
    private static void FitSubtitle(Label subtitle, int room)
    {
        var size = 9F;
        while (size >= 7.5F && TextSize(subtitle.Text, UiTheme.ShellFont(size)).Width + 16 > room)
            size -= 0.5F;
        subtitle.Font = UiTheme.ShellFont(size);
    }

    private static Size TextSize(string text, Font font)
    {
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.Left;
        return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), flags);
    }

    private static void LayoutCard(Control body, Control card)
    {
        var availW = Math.Max(300, body.ClientSize.Width - 64);
        var availH = Math.Max(320, body.ClientSize.Height - 48);
        var w = Math.Min(520, availW);
        var h = Math.Min(520, availH);
        card.SetBounds(
            (body.ClientSize.Width - w) / 2,
            Math.Max(24, (body.ClientSize.Height - h) / 2),
            w,
            h);
    }

    private void OnEnterKey(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;

        e.SuppressKeyPress = true;
        _ = LoginAsync();
    }

    private async Task LoginAsync()
    {
        if (_loggingIn)
            return;

        _loggingIn = true;
        _login.Loading = true;
        _login.Enabled = false;
        try
        {
            var result = await AppHost.Auth.LoginAsync(_user.Text, _password.Text, AppHost.DemoMode);
            if (!result.Success || result.Data == null)
            {
                UiNotify.Warn(this, result.Message);
                _password.SelectAll();
                _password.Focus();
                return;
            }

            Hide();
            using var main = new MainForm();
            main.ShowDialog(this);
            AppSession.SignOut();
            _password.Clear();
            Show();
            Activate();
            _user.Focus();
        }
        finally
        {
            _loggingIn = false;
            if (!IsDisposed)
            {
                _login.Loading = false;
                _login.Enabled = true;
            }
        }
    }
}
