namespace Mes.WinUI;

/// <summary>
/// 列表页顶栏：左侧放查询条件，右侧置顶增删改查按钮。
/// </summary>
public sealed class PageToolbar : AntdUI.Panel
{
    private readonly Panel _left = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.Transparent
    };

    private readonly FlowLayoutPanel _right = new()
    {
        Dock = DockStyle.Right,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(0, 16, UiTheme.SpaceMd, 0),
        BackColor = Color.Transparent
    };

    public PageToolbar()
    {
        Height = UiTheme.ToolbarHeight;
        Dock = DockStyle.Top;
        Back = UiTheme.CardBack;
        Radius = UiTheme.CardRadius;
        BorderWidth = 0;
        Shadow = 2;
        Padding = new Padding(UiTheme.SpaceMd, 0, 0, 0);
        Controls.Add(_left);
        Controls.Add(_right);
    }

    public Panel LeftHost => _left;

    public void AddActions(params Control[] buttons)
    {
        foreach (var button in buttons)
        {
            button.Margin = new Padding(UiTheme.SpaceSm, 0, 0, 0);
            if (button.Height < UiTheme.ControlHeight)
                button.Height = UiTheme.ControlHeight;
            if (button.Width < 108)
                button.Width = 108;
            _right.Controls.Add(button);
        }
    }

    public static AntdUI.Button ActionButton(
        string text,
        string? permission,
        AntdUI.TTypeMini type,
        int width = 108,
        string? iconSvg = null)
    {
        AntdUI.Button button = permission == null
            ? new AntdUI.Button()
            : new PermissionButton { PermissionCode = permission };
        button.Text = text;
        button.Type = type;
        button.Size = new Size(Math.Max(width, string.IsNullOrWhiteSpace(iconSvg) ? 96 : 108), UiTheme.ControlHeight);
        button.Font = UiTheme.UiFont();
        StyleIcon(button, iconSvg);
        return button;
    }

    /// <summary>统一按钮图标尺寸，避免 IconRatio 过大撑破按钮。</summary>
    public static void StyleIcon(AntdUI.Button button, string? iconSvg = null)
    {
        button.IconRatio = UiTheme.ButtonIconRatio;
        button.IconGap = 0.2F;
        if (!string.IsNullOrWhiteSpace(iconSvg))
            button.IconSvg = iconSvg;
    }
}
