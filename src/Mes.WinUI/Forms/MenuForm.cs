using System.ComponentModel;
using Mes.BLL;
using Mes.Common;
using Mes.Models;

namespace Mes.WinUI;

public class MenuForm : SecureForm
{
    private readonly AntdUI.Input _keyword = new() { PlaceholderText = "名称/编码/FormKey", AllowClear = true };
    private readonly AntdUI.Select _type = new() { PlaceholderText = "类型", List = true };
    private readonly DataGridView _grid = new();
    private readonly BindingList<MenuRow> _rows = [];
    private bool _busy;

    public MenuForm()
    {
        Text = "菜单管理";
        BackColor = UiTheme.PageBack;

        var toolbar = new PageToolbar();
        var kwLabel = FormUi.Caption("关键字", 0, 16, 64);
        kwLabel.TextAlign = ContentAlignment.MiddleLeft;
        _keyword.SetBounds(72, 16, 180, UiTheme.ControlHeight);
        var typeLabel = FormUi.Caption("类型", 268, 16, 40);
        typeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _type.SetBounds(312, 16, 120, UiTheme.ControlHeight);
        var search = new AntdUI.Button
        {
            Text = "查询",
            Type = AntdUI.TTypeMini.Primary,
            Bounds = new Rectangle(448, 16, 108, UiTheme.ControlHeight)
        };
        PageToolbar.StyleIcon(search, MenuIcons.Action.Search);
        search.Click += async (_, _) => await ReloadAsync();
        toolbar.LeftHost.Controls.AddRange(kwLabel, _keyword, typeLabel, _type, search);

        var add = PageToolbar.ActionButton("新增", "sys:menu:create", AntdUI.TTypeMini.Primary, iconSvg: MenuIcons.Action.Add);
        var edit = PageToolbar.ActionButton("修改", "sys:menu:edit", AntdUI.TTypeMini.Default, iconSvg: MenuIcons.Action.Edit);
        var delete = PageToolbar.ActionButton("删除", "sys:menu:delete", AntdUI.TTypeMini.Error, iconSvg: MenuIcons.Action.Delete);
        var refresh = PageToolbar.ActionButton("刷新", null, AntdUI.TTypeMini.Default, iconSvg: MenuIcons.Action.Refresh);
        add.Click += async (_, _) => await EditAsync(null);
        edit.Click += async (_, _) => await EditAsync(SelectedId());
        delete.Click += async (_, _) => await DeleteAsync();
        refresh.Click += async (_, _) => await ReloadAsync();
        toolbar.AddActions(add, edit, delete, refresh);

        ConfigureGrid();
        _grid.DataSource = _rows;
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex < 0 || !AppSession.HasPermission("sys:menu:edit")) return;
            await EditAsync(SelectedId());
        };

        var gridHost = UiChrome.CreateCard();
        gridHost.Margin = new Padding(0, UiTheme.SpaceSm, 0, 0);
        gridHost.Controls.Add(_grid);
        Controls.Add(gridHost);
        Controls.Add(toolbar);

        _type.Items.Add(new AntdUI.SelectItem("全部", -1));
        _type.Items.Add(new AntdUI.SelectItem("目录", (int)PermissionType.Directory));
        _type.Items.Add(new AntdUI.SelectItem("菜单", (int)PermissionType.Menu));
        _type.Items.Add(new AntdUI.SelectItem("按钮", (int)PermissionType.Button));
        _type.SelectedValue = -1;
        _type.Text = "全部";
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            PermissionType? type = _type.SelectedValue switch
            {
                int v when v > 0 => (PermissionType)v,
                _ => null
            };
            var result = await AppHost.Menus.QueryAsync(new MenuQuery
            {
                Keyword = _keyword.Text,
                PermType = type
            });
            if (!result.Success || result.Data == null)
            {
                UiNotify.Warn(OwnerWin, result.Message);
                return;
            }

            var byId = result.Data.ToDictionary(p => p.Id);
            _rows.RaiseListChangedEvents = false;
            _rows.Clear();
            foreach (var item in result.Data.OrderBy(p => p.Sort).ThenBy(p => p.Id))
            {
                _rows.Add(new MenuRow
                {
                    Id = item.Id,
                    ParentId = item.ParentId,
                    Name = item.PermName,
                    Code = item.PermCode,
                    TypeName = item.PermType switch
                    {
                        PermissionType.Directory => "目录",
                        PermissionType.Menu => "菜单",
                        _ => "按钮"
                    },
                    FormKey = item.FormKey ?? string.Empty,
                    Sort = item.Sort,
                    ParentName = item.ParentId == 0
                        ? "（根）"
                        : byId.TryGetValue(item.ParentId, out var parent) ? parent.PermName : item.ParentId.ToString()
                });
            }

            _rows.RaiseListChangedEvents = true;
            _rows.ResetBindings();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task EditAsync(long? id)
    {
        MenuSaveModel model;
        if (id == null)
        {
            model = new MenuSaveModel
            {
                ParentId = SelectedId() ?? 0,
                PermType = PermissionType.Menu,
                Sort = 10
            };
        }
        else
        {
            var loaded = await AppHost.Menus.GetAsync(id.Value);
            if (!loaded.Success || loaded.Data == null)
            {
                UiNotify.Warn(OwnerWin, loaded.Message);
                return;
            }

            var p = loaded.Data;
            model = new MenuSaveModel
            {
                Id = p.Id,
                ParentId = p.ParentId,
                PermName = p.PermName,
                PermCode = p.PermCode,
                PermType = p.PermType,
                FormKey = p.FormKey,
                Sort = p.Sort
            };
        }

        using var dlg = new MenuEditForm(model);
        if (dlg.ShowDialog(OwnerWin) == DialogResult.OK)
            await ReloadAsync();
    }

    private async Task DeleteAsync()
    {
        var id = SelectedId();
        if (id == null)
        {
            UiNotify.Info(OwnerWin, "请选择要删除的节点。");
            return;
        }

        var row = _grid.CurrentRow?.DataBoundItem as MenuRow;
        if (!UiNotify.Confirm(OwnerWin, $"确定删除「{row?.Name}」吗？", "菜单管理"))
            return;

        var result = await AppHost.Menus.DeleteAsync(id.Value);
        if (result.Success)
        {
            UiNotify.Success(OwnerWin, result.Message);
            await ReloadAsync();
        }
        else
        {
            UiNotify.Warn(OwnerWin, result.Message);
        }
    }

    private long? SelectedId() => _grid.CurrentRow?.DataBoundItem is MenuRow row ? row.Id : null;
    private Form OwnerWin => FindForm() is { } f && !ReferenceEquals(f, this) ? f : this;

    private void ConfigureGrid()
    {
        UiChrome.StyleGrid(_grid);
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(MenuRow.Name), HeaderText = "名称", FillWeight = 140 },
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(MenuRow.Code), HeaderText = "编码", FillWeight = 140 },
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(MenuRow.TypeName), HeaderText = "类型", FillWeight = 70 },
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(MenuRow.FormKey), HeaderText = "FormKey", FillWeight = 110 },
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(MenuRow.ParentName), HeaderText = "上级", FillWeight = 100 },
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(MenuRow.Sort), HeaderText = "排序", FillWeight = 60 });
    }

}

internal sealed class MenuRow
{
    public long Id { get; init; }
    public long ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string FormKey { get; init; } = string.Empty;
    public string ParentName { get; init; } = string.Empty;
    public int Sort { get; init; }
}
