using Mes.BLL;
using Mes.Models;

namespace Mes.WinUI;

public class MenuEditForm : AntdUI.Window
{
    private readonly MenuSaveModel _model;
    private readonly AntdUI.Input _name = new();
    private readonly AntdUI.Input _code = new();
    private readonly AntdUI.Select _type = new() { List = true };
    private readonly AntdUI.Select _parent = new() { List = true, AllowClear = true, PlaceholderText = "（根节点）" };
    private readonly AntdUI.Input _formKey = new() { PlaceholderText = "如 Sys.User" };
    private readonly AntdUI.InputNumber _sort = new() { Minimum = 0, Maximum = 9999, Value = 10 };
    private readonly AntdUI.Button _save = new()
    {
        Text = "保存",
        Type = AntdUI.TTypeMini.Primary,
        IconSvg = MenuIcons.Action.Save
    };
    private bool _saving;

    public MenuEditForm(MenuSaveModel model)
    {
        _model = model;
        Text = model.Id == 0 ? "新增菜单" : "修改菜单";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(540, 560);
        Resizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.PageBack;

        var header = UiChrome.CreateDialogHeader(Text);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceMd, UiTheme.SpaceXl, UiTheme.SpaceMd)
        };
        _save.Size = new Size(120, UiTheme.ControlHeight);
        PageToolbar.StyleIcon(_save);
        var cancel = new AntdUI.Button { Text = "取消", Type = AntdUI.TTypeMini.Default, Size = new Size(96, UiTheme.ControlHeight) };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _save.Click += async (_, _) => await SaveAsync();
        footer.Controls.Add(_save);
        footer.Controls.Add(cancel);
        footer.Resize += (_, _) =>
        {
            cancel.Location = new Point(footer.ClientSize.Width - cancel.Width, 12);
            _save.Location = new Point(cancel.Left - _save.Width - 12, 12);
        };

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceLg, UiTheme.SpaceXl, UiTheme.SpaceLg),
            BackColor = UiTheme.CardBack
        };

        var y = 8;
        FormUi.AddField(body, "名称", _name, ref y, 360);
        FormUi.AddField(body, "编码", _code, ref y, 360);
        FormUi.AddField(body, "类型", _type, ref y, 360);
        FormUi.AddField(body, "上级", _parent, ref y, 360);
        FormUi.AddField(body, "FormKey", _formKey, ref y, 360);
        FormUi.AddField(body, "排序", _sort, ref y, 360);

        _type.Items.Add(new AntdUI.SelectItem("目录", (int)PermissionType.Directory));
        _type.Items.Add(new AntdUI.SelectItem("菜单", (int)PermissionType.Menu));
        _type.Items.Add(new AntdUI.SelectItem("按钮", (int)PermissionType.Button));
        _type.SelectedValue = (int)_model.PermType;
        _type.Text = _model.PermType switch
        {
            PermissionType.Directory => "目录",
            PermissionType.Menu => "菜单",
            _ => "按钮"
        };
        _type.SelectedValueChanged += (_, _) => ToggleFormKey();

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
        Shown += async (_, _) => await BindAsync();
    }

    private async Task BindAsync()
    {
        _name.Text = _model.PermName;
        _code.Text = _model.PermCode;
        _formKey.Text = _model.FormKey ?? string.Empty;
        _sort.Value = _model.Sort;
        ToggleFormKey();

        var result = await AppHost.Menus.QueryAsync(new MenuQuery());
        if (!result.Success || result.Data == null)
        {
            UiNotify.Warn(this, result.Message);
            return;
        }

        _parent.Items.Clear();
        _parent.Items.Add(new AntdUI.SelectItem("（根节点）", 0L));
        foreach (var item in result.Data.Where(p => p.Id != _model.Id).OrderBy(p => p.Sort).ThenBy(p => p.Id))
        {
            var type = item.PermType switch
            {
                PermissionType.Directory => "目录",
                PermissionType.Menu => "菜单",
                _ => "按钮"
            };
            _parent.Items.Add(new AntdUI.SelectItem($"{item.PermName} [{type}]", item.Id));
        }

        _parent.SelectedValue = _model.ParentId;
        var selected = _parent.Items.OfType<AntdUI.SelectItem>()
            .FirstOrDefault(i => Equals(i.Tag, _model.ParentId) || Equals(Convert.ToInt64(i.Tag), _model.ParentId));
        _parent.Text = selected?.Text ?? "（根节点）";
    }

    private void ToggleFormKey()
    {
        var isMenu = _type.SelectedValue is int v && v == (int)PermissionType.Menu;
        _formKey.Enabled = isMenu;
        if (!isMenu)
            _formKey.Text = string.Empty;
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        _model.PermName = _name.Text;
        _model.PermCode = _code.Text;
        _model.PermType = _type.SelectedValue is int t ? (PermissionType)t : PermissionType.Menu;
        _model.ParentId = _parent.SelectedValue switch
        {
            long id => id,
            int id => id,
            _ => 0
        };
        _model.FormKey = _model.PermType == PermissionType.Menu ? _formKey.Text : null;
        _model.Sort = (int)_sort.Value;

        _saving = true;
        _save.Loading = true;
        try
        {
            var result = await AppHost.Menus.SaveAsync(_model);
            if (!result.Success)
            {
                UiNotify.Warn(this, result.Message);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        finally
        {
            _saving = false;
            if (!IsDisposed) _save.Loading = false;
        }
    }
}
