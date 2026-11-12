namespace Mes.WinUI;

/// <summary>系统设置：全局字体家族、字号、粗体。</summary>
public class SystemSettingsForm : SecureForm
{
    private readonly AntdUI.Select _fontFamily = new() { List = true };
    private readonly AntdUI.InputNumber _fontSize = new()
    {
        Minimum = 9,
        Maximum = 18,
        DecimalPlaces = 1,
        Increment = 0.5M
    };
    private readonly AntdUI.Checkbox _bold = new() { Text = "加粗显示" };
    private readonly Label _preview = new();

    public SystemSettingsForm()
    {
        Text = "系统设置";
        BackColor = UiTheme.PageBack;

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl)
        };

        var y = 8;
        FormUi.AddSection(card, "界面字体", ref y);

        var tip = FormUi.Hint("仅影响业务页签内的表格/表单字体；侧栏菜单、登录页与主框顶栏不受影响。新打开的业务页签立即生效。", 0, 0, 640);
        FormUi.Place(card, tip, 0, y, 640, 40);
        y += 48;

        FormUi.AddField(card, "字体", _fontFamily, ref y, 280);
        foreach (var name in new[]
                 {
                     "Microsoft YaHei UI",
                     "Microsoft YaHei",
                     "Segoe UI",
                     "SimSun",
                     "SimHei",
                     "KaiTi",
                     "FangSong",
                     "Arial",
                     "Tahoma"
                 })
            _fontFamily.Items.Add(name);

        FormUi.AddField(card, "字号", _fontSize, ref y, 120);
        FormUi.Place(card, _bold, FormUi.EditorLeft + 140, y - FormUi.RowHeight + 8, 120, UiTheme.ControlHeight);

        _preview.TextAlign = ContentAlignment.MiddleLeft;
        _preview.ForeColor = UiTheme.TextPrimary;
        _preview.BackColor = Color.Transparent;
        FormUi.Place(card, _preview, 0, y, 560, 48);
        y += 56;

        var save = new AntdUI.Button
        {
            Text = "保存并应用",
            Type = AntdUI.TTypeMini.Primary,
            Size = new Size(148, UiTheme.ControlHeight)
        };
        PageToolbar.StyleIcon(save, MenuIcons.Action.Save);
        save.Click += (_, _) => Save();

        var reset = new AntdUI.Button
        {
            Text = "恢复默认",
            Type = AntdUI.TTypeMini.Default,
            Size = new Size(120, UiTheme.ControlHeight)
        };
        reset.Click += (_, _) =>
        {
            LoadValues(new UiSettings());
            RefreshPreview();
        };

        FormUi.Place(card, save, 0, y, 148, UiTheme.ControlHeight);
        FormUi.Place(card, reset, 160, y, 120, UiTheme.ControlHeight);

        _fontFamily.SelectedValueChanged += (_, _) => RefreshPreview();
        _fontSize.ValueChanged += (_, _) => RefreshPreview();
        _bold.CheckedChanged += (_, _) => RefreshPreview();

        Controls.Add(card);

        LoadValues(UiSettings.Current);
        RefreshPreview();
    }

    private void LoadValues(UiSettings settings)
    {
        _fontFamily.Text = settings.FontFamily;
        _fontSize.Value = (decimal)settings.FontSize;
        _bold.Checked = settings.FontBold;
    }

    private UiSettings ReadValues() => new()
    {
        FontFamily = string.IsNullOrWhiteSpace(_fontFamily.Text) ? "Microsoft YaHei UI" : _fontFamily.Text.Trim(),
        FontSize = (float)_fontSize.Value,
        FontBold = _bold.Checked
    };

    private void RefreshPreview()
    {
        var s = ReadValues();
        _preview.Font = new Font(s.FontFamily, s.FontSize, s.FontBold ? FontStyle.Bold : FontStyle.Regular);
        _preview.Text = "预览：APEX-MES 系统管理 · 用户管理 Aa 123";
    }

    private void Save()
    {
        var settings = ReadValues();
        UiSettings.Save(settings);
        UiTheme.ApplySettings(settings);
        RefreshPreview();
        Font = UiTheme.ContentFont();
        UiNotify.Success(this, "业务页字体已保存。请重新打开业务页签查看效果；菜单与登录页不受影响。");
    }
}
