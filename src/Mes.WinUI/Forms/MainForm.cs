using Mes.Common;
using Mes.Models;

namespace Mes.WinUI;

public class MainForm : AntdUI.Window
{
    private readonly SideNavPanel _nav = new() { Dock = DockStyle.Fill };
    private readonly AntdUI.Tabs _tabs = new()
    {
        Dock = DockStyle.Fill,
        Type = AntdUI.TabType.Card,
        Gap = 10
    };

    private readonly Label _userChip = new()
    {
        Font = UiTheme.ShellFont(),
        ForeColor = UiTheme.HeaderText,
        Dock = DockStyle.Right,
        Width = 240,
        TextAlign = ContentAlignment.MiddleRight,
        BackColor = Color.Transparent,
        Padding = new Padding(0, 0, 8, 0)
    };

    private readonly Label _clock = new()
    {
        Font = UiTheme.ShellFont(),
        ForeColor = UiTheme.HeaderText,
        Dock = DockStyle.Right,
        Width = 190,
        TextAlign = ContentAlignment.MiddleRight,
        BackColor = Color.Transparent,
        Padding = new Padding(0, 0, 12, 0)
    };

    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };

    private readonly Dictionary<string, MenuNodeDto> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly TabManager _tabsManager;
    private bool _opening;

    public MainForm()
    {
        Text = AppBrand.FullName;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1360, 860);
        MinimumSize = new Size(1100, 680);
        BackColor = UiTheme.PageBack;
        _tabsManager = new TabManager(_tabs);

        // 右侧留白加大，登录信息/时间整体靠右但不贴边
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = UiTheme.HeaderHeight,
            BackColor = UiTheme.HeaderBack,
            Padding = new Padding(UiTheme.SpaceLg, 0, 32, 0)
        };

        _userChip.Text = $"{AppSession.RealName}  ·  {AppSession.UserName}";
        TickClock();

        var logout = new AntdUI.Button
        {
            Text = "退出",
            Type = AntdUI.TTypeMini.Default,
            IconSvg = MenuIcons.Action.Logout,
            Font = UiTheme.ShellFont(),
            Dock = DockStyle.Right,
            Width = 108,
            Margin = new Padding(UiTheme.SpaceMd, 18, 0, 18)
        };
        PageToolbar.StyleIcon(logout);
        logout.Font = UiTheme.ShellFont();
        logout.Click += (_, _) => Close();

        // Dock Right：后添加的先靠右边缘 → 视觉顺序：用户 | 时间 | 退出
        header.Controls.Add(_userChip);
        header.Controls.Add(_clock);
        header.Controls.Add(logout);

        var sider = new Panel
        {
            Dock = DockStyle.Left,
            Width = UiTheme.SiderWidth,
            BackColor = UiTheme.SiderBack,
            Padding = new Padding(0)
        };

        var brand = new Panel
        {
            Dock = DockStyle.Top,
            Height = UiTheme.HeaderHeight,
            BackColor = Color.FromArgb(0, 17, 34),
            Padding = new Padding(UiTheme.SpaceMd, 0, UiTheme.SpaceMd, 0)
        };
        brand.Controls.Add(new Label
        {
            Text = AppBrand.Name,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White,
            Font = UiTheme.ShellFont(UiTheme.ShellFontSizeTitle, FontStyle.Bold),
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 0, 0)
        });

        sider.Controls.Add(_nav);
        sider.Controls.Add(brand);

        var body = new TechBackPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiTheme.SpaceLg),
            BackdropOpacity = 0.7f,
            WashColor = Color.FromArgb(90, 230, 240, 250)
        };
        _tabs.BackColor = UiTheme.PageBack;
        body.Controls.Add(_tabs);

        Controls.Add(body);
        Controls.Add(header);
        Controls.Add(sider);

        IndexRoutes(AppSession.Menus);
        _nav.Bind(AppSession.Menus, collapseGroups: true);
        _nav.ItemSelected += Open;
        _tabs.SelectedIndexChanged += (_, _) => _nav.SetActive(_tabs.SelectedTab?.Name);

        _tabsManager.OpenOrActivate("Home", "首页", () => FormFactory.Create("Home"), closable: false);
        _nav.SetActive("Home");

        _clockTimer.Tick += (_, _) => TickClock();
        _clockTimer.Start();
        FormClosed += (_, _) =>
        {
            _clockTimer.Stop();
            _clockTimer.Dispose();
        };
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        WindowState = FormWindowState.Maximized;
    }

    private void TickClock()
    {
        _clock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void IndexRoutes(IEnumerable<MenuNodeDto> menus)
    {
        foreach (var menu in menus)
        {
            if (!string.IsNullOrWhiteSpace(menu.FormKey))
                _routes[menu.FormKey!] = menu;
            if (menu.Children.Count > 0)
                IndexRoutes(menu.Children);
        }
    }

    private void Open(MenuNodeDto node)
    {
        if (_opening || string.IsNullOrWhiteSpace(node.FormKey))
            return;

        if (!string.IsNullOrWhiteSpace(node.PermissionCode) && !AppSession.HasPermission(node.PermissionCode))
        {
            UiNotify.Warn(this, "没有访问该功能的权限。");
            return;
        }

        _opening = true;
        try
        {
            var route = _routes.TryGetValue(node.FormKey!, out var mapped) ? mapped : node;
            var title = string.IsNullOrWhiteSpace(route.Title) ? node.Title : route.Title;
            if (string.IsNullOrWhiteSpace(title))
                title = route.FormKey ?? "未命名";
            _tabsManager.OpenOrActivate(route.FormKey!, title, () => FormFactory.Create(route.FormKey!));
            _nav.SetActive(route.FormKey);
        }
        catch (Exception ex)
        {
            Mes.Common.ErrorLog.Write(ex, $"打开菜单 {node.FormKey}");
            UiNotify.Error(this, $"打开「{node.Title}」失败：{ex.Message}");
        }
        finally
        {
            _opening = false;
        }
    }
}
