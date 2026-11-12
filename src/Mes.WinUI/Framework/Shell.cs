using Mes.Models;

namespace Mes.WinUI;

public static class FormFactory
{
    public static Control Create(string formKey) => formKey switch
    {
        "Home" => new UcHome(),
        "Sys.User" => Host(new UserForm()),
        "Sys.Role" => Host(new RoleForm()),
        "Sys.Menu" => Host(new MenuForm()),
        "Sys.Settings" => Host(new SystemSettingsForm()),
        "Mes.WorkOrder" => Host(new WorkOrderForm()),
        "Mes.PowerBi" => Host(new PowerBiReportForm()),
        "Mes.LabelPrint" => Host(new LabelPrintForm()),
        "Mes.Station" => Host(new StationGateForm()),
        _ => new UcModule($"未注册模块：{formKey}")
    };

    private static Control Host(Form form)
    {
        // 先装进 Panel；真正 Show 放到加入 Tab 且句柄就绪之后
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        form.Visible = false;

        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.PageBack
        };
        host.Controls.Add(form);
        return host;
    }
}

public sealed class TabManager
{
    private readonly AntdUI.Tabs _tabs;
    private readonly Dictionary<string, AntdUI.TabPage> _opened = new(StringComparer.OrdinalIgnoreCase);

    public TabManager(AntdUI.Tabs tabs)
    {
        _tabs = tabs;
        _tabs.Type = AntdUI.TabType.Card;
        _tabs.CloseDisposePage = true;
        _tabs.BackColor = UiTheme.PageBack;
        _tabs.Font = UiTheme.ShellFont();
        // AntdUI：Tabs.Fill/FillHover 是选中/悬停「文字色」，不是内容区背景
        _tabs.ForeColor = UiTheme.TextPrimary;
        _tabs.Fill = UiTheme.Primary;
        _tabs.FillHover = UiTheme.PrimaryHover;
        _tabs.Style = new AntdUI.Tabs.StyleCard
        {
            Closable = true,
            Radius = 8,
            Gap = 8,
            Fill = Color.FromArgb(245, 245, 245),
            FillHover = Color.FromArgb(230, 244, 255),
            FillActive = UiTheme.CardBack,
            BorderColor = UiTheme.Border,
            BorderActive = UiTheme.Primary
        };
        _tabs.ClosingPage += OnClosingPage;
    }

    public void OpenOrActivate(string key, string title, Func<Control> createContent, bool closable = true)
    {
        if (_opened.TryGetValue(key, out var exist))
        {
            if (!string.IsNullOrWhiteSpace(title))
                exist.Text = title;
            Activate(exist);
            return;
        }

        var displayTitle = string.IsNullOrWhiteSpace(title) ? key : title.Trim();
        var page = new AntdUI.TabPage
        {
            Name = key,
            Text = displayTitle,
            Tag = closable,
            ReadOnly = !closable,
            BackColor = UiTheme.PageBack
        };

        Control content;
        try
        {
            content = createContent();
        }
        catch (Exception ex)
        {
            Mes.Common.ErrorLog.Write(ex, $"打开页面 {key}");
            UiNotify.Error(null, $"无法打开「{displayTitle}」：{ex.Message}");
            return;
        }

        // 嵌入窗体标题可作为兜底，避免页签 Text 为空
        if (string.IsNullOrWhiteSpace(page.Text) || page.Text == key)
        {
            var embedded = FindFirstForm(content);
            if (embedded is { Text.Length: > 0 })
                displayTitle = embedded.Text;
        }

        page.Text = displayTitle;
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        _tabs.Pages.Add(page);
        page.Text = displayTitle; // 加入集合后再写一次，防止控件树同步冲掉标题
        _opened[key] = page;
        Activate(page);
        ShowEmbeddedForms(content);
        ScheduleRefresh(content);
    }

    private static Form? FindFirstForm(Control root)
    {
        foreach (var c in Enumerate(root))
        {
            if (c is Form f)
                return f;
        }

        return null;
    }

    private void Activate(AntdUI.TabPage page)
    {
        try
        {
            _tabs.SelectedTab = page;
        }
        catch
        {
            var index = _tabs.Pages.IndexOf(page);
            if (index >= 0)
                _tabs.SelectedIndex = index;
        }
    }

    private void ScheduleRefresh(Control content)
    {
        void Refresh()
        {
            if (content.IsDisposed)
                return;
            ShowEmbeddedForms(content);
            content.PerformLayout();
            content.Invalidate(true);
        }

        // 句柄未创建时不能 BeginInvoke（登录后立刻开首页会踩中）
        if (content.IsHandleCreated)
        {
            content.BeginInvoke((Action)Refresh);
            return;
        }

        if (_tabs.IsHandleCreated)
        {
            _tabs.BeginInvoke((Action)Refresh);
            return;
        }

        void OnHandleCreated(object? sender, EventArgs e)
        {
            _tabs.HandleCreated -= OnHandleCreated;
            if (!_tabs.IsDisposed && _tabs.IsHandleCreated)
                _tabs.BeginInvoke((Action)Refresh);
            else
                Refresh();
        }

        _tabs.HandleCreated += OnHandleCreated;
    }

    private static void ShowEmbeddedForms(Control root)
    {
        foreach (var control in Enumerate(root))
        {
            if (control is Form form && !form.IsDisposed)
            {
                form.Dock = DockStyle.Fill;
                if (!form.Visible)
                    form.Show();
                form.BringToFront();
            }
        }
    }

    public void Close(string key)
    {
        if (!_opened.TryGetValue(key, out var page))
            return;
        if (page.Tag is false)
            return;

        _tabs.Pages.Remove(page);
        _opened.Remove(key);
        page.Dispose();
    }

    private bool OnClosingPage(object? sender, AntdUI.ClosingPageEventArgs e)
    {
        var page = e.Value;
        if (page.Tag is false || page.ReadOnly)
            return false;

        _opened.Remove(page.Name);
        return true;
    }

    private static IEnumerable<Control> Enumerate(Control root)
    {
        yield return root;
        foreach (Control child in root.Controls)
        foreach (var nested in Enumerate(child))
            yield return nested;
    }
}
