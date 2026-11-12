namespace Mes.WinUI;

/// <summary>Admin.NET / Ant Design Pro 气质的全局 Token。</summary>
public static class UiTheme
{
    // —— 颜色 ——
    public static readonly Color Primary = Color.FromArgb(22, 119, 255);
    public static readonly Color PrimaryHover = Color.FromArgb(64, 150, 255);
    public static readonly Color SiderBack = Color.FromArgb(0, 21, 41);
    public static readonly Color SiderText = Color.FromArgb(255, 255, 255);
    public static readonly Color SiderMuted = Color.FromArgb(160, 174, 192);
    public static readonly Color SiderHover = Color.FromArgb(0, 33, 64);
    public static readonly Color HeaderBack = Color.White;
    public static readonly Color HeaderText = Color.FromArgb(31, 31, 31);
    public static readonly Color DialogHeaderBack = Color.FromArgb(22, 119, 255);
    public static readonly Color PageBack = Color.FromArgb(240, 242, 245);
    public static readonly Color CardBack = Color.White;
    public static readonly Color Border = Color.FromArgb(235, 237, 240);
    public static readonly Color TableHeader = Color.FromArgb(250, 250, 250);
    public static readonly Color RowHover = Color.FromArgb(230, 244, 255);
    public static readonly Color TextPrimary = Color.FromArgb(38, 38, 38);
    public static readonly Color MutedText = Color.FromArgb(140, 140, 140);

    // —— 壳层固定字体（侧栏 / 登录 / 主框顶栏等，不受系统设置影响）——
    public const string ShellFontFamily = "Microsoft YaHei UI";
    public const float ShellFontSize = 10.5F;
    public const float ShellFontSizeSm = 9.5F;
    public const float ShellFontSizeLg = 13F;
    public const float ShellFontSizeTitle = 15F;
    public const float MenuFontSize = 13.5F;
    public const float MenuGroupFontSize = 14.5F;

    // —— 业务内容字体（仅系统设置可调）——
    public static string ContentFontFamily { get; private set; } = ShellFontFamily;
    public static float ContentFontSize { get; private set; } = ShellFontSize;
    public static bool ContentFontBold { get; private set; }

    public static string FontFamily => ContentFontFamily;
    public static float FontSize => ContentFontSize;
    public static bool FontBold => ContentFontBold;
    public static float FontSizeSm => Math.Max(9F, ContentFontSize - 1F);
    public static float FontSizeLg => ContentFontSize + 2.5F;
    public static float FontSizeTitle => ContentFontSize + 4.5F;

    public const float MenuIconRatio = 1.2F;
    public const float ButtonIconRatio = 0.72F;
    public const float IconRatio = ButtonIconRatio;
    public const int SpaceSm = 8;
    public const int SpaceMd = 12;
    public const int SpaceLg = 16;
    public const int SpaceXl = 24;
    public const int ToolbarHeight = 78;
    public const int HeaderHeight = 90;
    public const int SiderWidth = 256;
    public const int ControlHeight = 44;
    public const int TableHeaderHeight = 46;
    public const int TableRowHeight = 42;
    public const int CardRadius = 10;
    public const int CardShadow = 6;
    public const int PagerHeight = 60;
    public const float MenuWidthRatio = 0.9F;

    /// <summary>壳层字体：菜单外的登录页、主框顶栏等。</summary>
    public static Font ShellFont(float? size = null, FontStyle style = FontStyle.Regular)
        => new(ShellFontFamily, size ?? ShellFontSize, style);

    /// <summary>侧栏菜单字体（固定偏大，视觉更充实）。</summary>
    public static Font MenuFont(FontStyle style = FontStyle.Regular)
        => new(ShellFontFamily, MenuFontSize, style);

    public static Font MenuGroupFont()
        => new(ShellFontFamily, MenuGroupFontSize, FontStyle.Bold);

    /// <summary>业务内容字体：受系统设置影响。</summary>
    public static Font ContentFont(float? size = null, FontStyle? style = null)
    {
        var st = style ?? (ContentFontBold ? FontStyle.Bold : FontStyle.Regular);
        return new Font(ContentFontFamily, size ?? ContentFontSize, st);
    }

    /// <summary>业务内容字体（同 ContentFont，兼容旧调用）。</summary>
    public static Font UiFont(float? size = null, FontStyle? style = null) => ContentFont(size, style);

    public static void Apply()
    {
        UiSettings.Load();
        ApplySettings(UiSettings.Current);
        AntdUI.Config.IsDark = false;
        AntdUI.Config.TextRenderingHighQuality = true;
        AntdUI.Config.ShowInWindowByMessage = true;
        AntdUI.Style.SetPrimary(Primary);
        // 默认字体固定为壳层，避免设置改动波及登录/菜单等
        Application.SetDefaultFont(ShellFont());
    }

    public static void ApplySettings(UiSettings settings, bool setDefaultFont = false)
    {
        ContentFontFamily = string.IsNullOrWhiteSpace(settings.FontFamily) ? ShellFontFamily : settings.FontFamily;
        ContentFontSize = Math.Clamp(settings.FontSize, 9F, 18F);
        ContentFontBold = settings.FontBold;
        if (setDefaultFont)
            Application.SetDefaultFont(ShellFont());
    }
}
