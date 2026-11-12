using System.ComponentModel;
using Mes.BLL;
using Mes.Common;
using Mes.Models;

namespace Mes.WinUI;

public class RoleForm : SecureForm
{
    private const int PageSize = 20;
    private readonly AntdUI.Input _keyword = new() { PlaceholderText = "角色名/编码", AllowClear = true };
    private readonly DataGridView _grid = new();
    private readonly AntdUI.Pagination _pager = new() { PageSize = PageSize, ShowSizeChanger = false, Dock = DockStyle.Fill };
    private readonly BindingList<RoleListItem> _rows = [];
    private int _page = 1;
    private int _total;
    private bool _busy;

    public RoleForm()
    {
        Text = "角色管理";
        BackColor = UiTheme.PageBack;

        var toolbar = new PageToolbar();
        var label = FormUi.Caption("关键字", 0, 16, 64);
        label.TextAlign = ContentAlignment.MiddleLeft;
        _keyword.SetBounds(72, 16, 220, UiTheme.ControlHeight);
        var search = new AntdUI.Button
        {
            Text = "查询",
            Type = AntdUI.TTypeMini.Primary,
            Bounds = new Rectangle(304, 16, 108, UiTheme.ControlHeight)
        };
        PageToolbar.StyleIcon(search, MenuIcons.Action.Search);
        search.Click += async (_, _) => { _page = 1; await ReloadAsync(); };
        toolbar.LeftHost.Controls.AddRange(label, _keyword, search);

        var add = PageToolbar.ActionButton("新增", "sys:role:create", AntdUI.TTypeMini.Primary, iconSvg: MenuIcons.Action.Add);
        var edit = PageToolbar.ActionButton("修改/授权", "sys:role:edit", AntdUI.TTypeMini.Default, 120, MenuIcons.Action.Edit);
        var delete = PageToolbar.ActionButton("删除", "sys:role:delete", AntdUI.TTypeMini.Error, iconSvg: MenuIcons.Action.Delete);
        var refresh = PageToolbar.ActionButton("刷新", null, AntdUI.TTypeMini.Default, iconSvg: MenuIcons.Action.Refresh);
        add.Click += async (_, _) => await EditAsync(null);
        edit.Click += async (_, _) => await EditAsync(SelectedId());
        delete.Click += async (_, _) => await DeleteAsync();
        refresh.Click += async (_, _) => await ReloadAsync();
        toolbar.AddActions(add, edit, delete, refresh);

        var pagerBar = UiChrome.CreatePagerBar();
        pagerBar.Controls.Add(_pager);
        _pager.ValueChanged += async (_, e) =>
        {
            if (_busy || e.Current == _page) return;
            _page = Math.Max(1, e.Current);
            await ReloadAsync();
        };

        ConfigureGrid();
        _grid.DataSource = _rows;
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex < 0 || !AppSession.HasPermission("sys:role:edit")) return;
            await EditAsync(SelectedId());
        };

        var gridHost = UiChrome.CreateCard();
        gridHost.Controls.Add(_grid);
        Controls.Add(gridHost);
        Controls.Add(pagerBar);
        Controls.Add(toolbar);
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
            while (!IsDisposed)
            {
                var result = await AppHost.Roles.QueryAsync(new RoleQuery
                {
                    Keyword = _keyword.Text,
                    PageIndex = _page,
                    PageSize = PageSize
                });
                if (!result.Success || result.Data == null)
                {
                    UiNotify.Warn(OwnerWin, result.Message);
                    return;
                }

                _total = result.Data.Total;
                var pages = Math.Max(1, (int)Math.Ceiling(_total / (double)PageSize));
                if (_page > pages) { _page = pages; continue; }

                _rows.RaiseListChangedEvents = false;
                _rows.Clear();
                foreach (var item in result.Data.Items)
                    _rows.Add(item);
                _rows.RaiseListChangedEvents = true;
                _rows.ResetBindings();
                break;
            }
        }
        finally
        {
            _busy = false;
            _pager.Total = _total;
            _pager.Current = _page;
        }
    }

    private async Task EditAsync(long? id)
    {
        RoleSaveModel model;
        if (id == null)
        {
            model = new RoleSaveModel { Enabled = true };
        }
        else
        {
            var loaded = await AppHost.Roles.GetAsync(id.Value);
            if (!loaded.Success || loaded.Data == null)
            {
                UiNotify.Warn(OwnerWin, loaded.Message);
                return;
            }

            model = loaded.Data;
        }

        using var dlg = new RoleEditForm(model);
        if (dlg.ShowDialog(OwnerWin) == DialogResult.OK)
            await ReloadAsync();
    }

    private async Task DeleteAsync()
    {
        var id = SelectedId();
        if (id == null)
        {
            UiNotify.Info(OwnerWin, "请选择要删除的角色。");
            return;
        }

        var row = _grid.CurrentRow?.DataBoundItem as RoleListItem;
        if (!UiNotify.Confirm(OwnerWin, $"确定删除角色 {row?.RoleName} 吗？", "角色管理"))
            return;

        var result = await AppHost.Roles.DeleteAsync(id.Value);
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

    private long? SelectedId() => _grid.CurrentRow?.DataBoundItem is RoleListItem row ? row.Id : null;
    private Form OwnerWin => FindForm() is { } f && !ReferenceEquals(f, this) ? f : this;

    private void ConfigureGrid()
    {
        UiChrome.StyleGrid(_grid);
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(RoleListItem.RoleName), HeaderText = "角色名", FillWeight = 120 },
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(RoleListItem.RoleCode), HeaderText = "编码", FillWeight = 100 },
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(RoleListItem.Enabled), HeaderText = "启用", FillWeight = 60 },
            new DataGridViewTextBoxColumn { DataPropertyName = nameof(RoleListItem.PermissionCount), HeaderText = "权限数", FillWeight = 80 });
    }

}
