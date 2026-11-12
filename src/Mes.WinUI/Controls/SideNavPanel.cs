using Mes.Models;

namespace Mes.WinUI;

/// <summary>
/// 深色侧栏导航。分组默认可收起；叶子项横向约占侧栏 90% 宽度。
/// </summary>
public sealed class SideNavPanel : Panel
{
    private readonly Panel _host = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        BackColor = UiTheme.SiderBack
    };

    private readonly Dictionary<string, AntdUI.Button> _leafButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<NavGroup> _groups = [];

    public event Action<MenuNodeDto>? ItemSelected;

    public SideNavPanel()
    {
        BackColor = UiTheme.SiderBack;
        Controls.Add(_host);
        Resize += (_, _) => ApplyMenuInsets();
        HandleCreated += (_, _) => ApplyMenuInsets();
    }

    private void ApplyMenuInsets()
    {
        // 左右各留 5%，内容区约为侧栏宽度的 90%
        var w = Math.Max(Width, UiTheme.SiderWidth);
        var side = Math.Max(8, (int)Math.Round(w * (1f - UiTheme.MenuWidthRatio) / 2f));
        _host.Padding = new Padding(side, 4, side, 6);
    }

    public void Bind(IEnumerable<MenuNodeDto> menus, bool collapseGroups = true)
    {
        ApplyMenuInsets();
        _host.SuspendLayout();
        _host.Controls.Clear();
        _leafButtons.Clear();
        _groups.Clear();

        var blocks = new List<Control>();
        foreach (var node in menus)
            blocks.AddRange(CreateBlocks(node, level: 0, collapseGroups));

        for (var i = blocks.Count - 1; i >= 0; i--)
            _host.Controls.Add(blocks[i]);

        _host.ResumeLayout(true);
    }

    public void SetActive(string? formKey)
    {
        foreach (var (key, button) in _leafButtons)
        {
            var active = !string.IsNullOrWhiteSpace(formKey)
                         && key.Equals(formKey, StringComparison.OrdinalIgnoreCase);
            button.Type = active ? AntdUI.TTypeMini.Primary : AntdUI.TTypeMini.Default;
            button.Ghost = !active;
            button.ForeColor = active ? Color.White : Color.FromArgb(220, 230, 240);
            button.BackColor = active ? UiTheme.Primary : Color.Transparent;
        }

        if (string.IsNullOrWhiteSpace(formKey))
            return;
        foreach (var group in _groups)
        {
            if (group.Contains(formKey))
                group.SetExpanded(true);
        }
    }

    private IEnumerable<Control> CreateBlocks(MenuNodeDto node, int level, bool collapseGroups)
    {
        if (node.Children.Count > 0)
        {
            var group = CreateGroup(node, level, expanded: !collapseGroups);
            _groups.Add(group);
            yield return group.Root;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(node.FormKey))
            yield break;

        yield return CreateLeafButton(node, level);
    }

    private NavGroup CreateGroup(MenuNodeDto node, int level, bool expanded)
    {
        var root = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 0)
        };

        var body = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Visible = expanded,
            Padding = new Padding(4, 0, 0, 0)
        };

        var childKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var childBlocks = new List<Control>();
        foreach (var child in node.Children)
        {
            CollectKeys(child, childKeys);
            childBlocks.AddRange(CreateBlocks(child, level + 1, collapseGroups: true));
        }

        for (var i = childBlocks.Count - 1; i >= 0; i--)
            body.Controls.Add(childBlocks[i]);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Padding = new Padding(4 + level * 6, 0, 0, 0)
        };

        var chevron = new AntdUI.Button
        {
            Type = AntdUI.TTypeMini.Default,
            Ghost = true,
            Dock = DockStyle.Right,
            Width = 36,
            BorderWidth = 0,
            Radius = 6,
            IconSvg = expanded ? "DownOutlined" : "RightOutlined",
            IconRatio = 0.85F,
            ForeColor = Color.FromArgb(160, 180, 200),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var leftIcon = new AntdUI.Button
        {
            Type = AntdUI.TTypeMini.Default,
            Ghost = true,
            Dock = DockStyle.Left,
            Width = 36,
            BorderWidth = 0,
            Radius = 6,
            IconSvg = MenuIcons.Resolve(node.FormKey, node.Title),
            IconRatio = 0.95F,
            ForeColor = Color.FromArgb(180, 200, 220),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var title = new Label
        {
            Text = WithLetterSpacing(node.Title),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 4, 0),
            Font = UiTheme.MenuGroupFont(),
            ForeColor = Color.FromArgb(210, 220, 230),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var group = new NavGroup(root, title, chevron, body, childKeys, node.Title);
        void Toggle(object? _, EventArgs __) => group.Toggle();
        header.Click += Toggle;
        title.Click += Toggle;
        leftIcon.Click += Toggle;
        chevron.Click += Toggle;

        // Dock：先 Fill 再 Left/Right → 左右图标贴边，标题居中填充
        header.Controls.Add(title);
        header.Controls.Add(leftIcon);
        header.Controls.Add(chevron);
        root.Controls.Add(body);
        root.Controls.Add(header);
        return group;
    }

    /// <summary>中文字符间插入空格，模拟字间距。</summary>
    private static string WithLetterSpacing(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        // 四分之一 em 空格，比普通空格更紧凑仍能拉开字距
        const string gap = "\u2005";
        return string.Join(gap, text.Where(c => !char.IsWhiteSpace(c)));
    }

    private static void CollectKeys(MenuNodeDto node, ISet<string> keys)
    {
        if (!string.IsNullOrWhiteSpace(node.FormKey))
            keys.Add(node.FormKey!);
        foreach (var child in node.Children)
            CollectKeys(child, keys);
    }

    private Control CreateLeafButton(MenuNodeDto node, int level)
    {
        var padLeft = 6 + level * 8;
        // 直接 Dock Top，不再套一层 Panel；纵向 Padding/Margin 为 0，按钮彼此贴紧
        var button = new AntdUI.Button
        {
            Text = node.Title,
            IconSvg = MenuIcons.Resolve(node.FormKey, node.Title),
            Type = AntdUI.TTypeMini.Default,
            Ghost = true,
            Dock = DockStyle.Top,
            Height = 40,
            Margin = Padding.Empty,
            Padding = new Padding(padLeft, 0, 8, 0),
            Font = UiTheme.MenuFont(),
            ForeColor = Color.FromArgb(220, 230, 240),
            BackColor = Color.Transparent,
            Radius = 6,
            BorderWidth = 0,
            TextAlign = ContentAlignment.MiddleLeft,
            IconRatio = 0.62F,
            IconGap = 0.28F,
            Tag = node
        };
        PageToolbar.StyleIcon(button);
        button.Dock = DockStyle.Top;
        button.Height = 40;
        button.Margin = Padding.Empty;
        button.Padding = new Padding(padLeft, 0, 8, 0);
        button.IconRatio = 0.62F;
        button.Font = UiTheme.MenuFont();
        button.Click += (_, _) =>
        {
            if (button.Tag is MenuNodeDto menu)
                ItemSelected?.Invoke(menu);
        };

        if (!string.IsNullOrWhiteSpace(node.FormKey))
            _leafButtons[node.FormKey!] = button;

        return button;
    }

    private sealed class NavGroup(
        Panel root,
        Label title,
        AntdUI.Button chevron,
        Panel body,
        HashSet<string> keys,
        string rawTitle)
    {
        public Panel Root { get; } = root;

        public bool Contains(string formKey) => keys.Contains(formKey);

        public void Toggle() => SetExpanded(!body.Visible);

        public void SetExpanded(bool value)
        {
            body.Visible = value;
            title.Text = WithLetterSpacing(rawTitle);
            chevron.IconSvg = value ? "DownOutlined" : "RightOutlined";
            Root.PerformLayout();
        }
    }
}
