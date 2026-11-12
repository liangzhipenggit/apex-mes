using System.ComponentModel;
using Mes.BLL;
using Mes.Common;
using Mes.Models;
using ErrorLog = Mes.Common.ErrorLog;

namespace Mes.WinUI;

public class UserForm : SecureForm
{
    private const int PageSize = 20;
    private readonly AntdUI.Input _keyword = new() { PlaceholderText = "用户名/姓名", AllowClear = true };
    private readonly DataGridView _grid = new();
    private readonly AntdUI.Pagination _pager = new() { PageSize = PageSize, ShowSizeChanger = false, Dock = DockStyle.Fill };
    private readonly BindingList<UserListItem> _rows = [];
    private int _page = 1;
    private int _total;
    private bool _busy;

    public UserForm()
    {
        Text = "用户管理";
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

        var add = PageToolbar.ActionButton("新增", "sys:user:create", AntdUI.TTypeMini.Primary, iconSvg: MenuIcons.Action.Add);
        var edit = PageToolbar.ActionButton("修改/角色", "sys:user:edit", AntdUI.TTypeMini.Default, 120, MenuIcons.Action.Edit);
        var delete = PageToolbar.ActionButton("删除", "sys:user:delete", AntdUI.TTypeMini.Error, iconSvg: MenuIcons.Action.Delete);
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
            if (e.RowIndex < 0 || !AppSession.HasPermission("sys:user:edit")) return;
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
                var result = await AppHost.Users.QueryAsync(new UserQuery
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
        try
        {
            UserSaveModel model;
            if (id == null)
            {
                model = new UserSaveModel { Enabled = true };
            }
            else
            {
                var loaded = await AppHost.Users.GetAsync(id.Value);
                if (!loaded.Success || loaded.Data == null)
                {
                    UiNotify.Warn(OwnerWin, loaded.Message);
                    return;
                }

                model = loaded.Data;
            }

            using var dlg = new UserEditForm(model);
            var owner = OwnerWin;
            if (dlg.ShowDialog(owner) == DialogResult.OK)
                await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "打开用户编辑窗口");
            UiNotify.Error(OwnerWin, "无法打开用户编辑窗口：" + ex.Message);
        }
    }

    private async Task DeleteAsync()
    {
        var id = SelectedId();
        if (id == null)
        {
            UiNotify.Info(OwnerWin, "请选择要删除的用户。");
            return;
        }

        var row = _grid.CurrentRow?.DataBoundItem as UserListItem;
        if (!UiNotify.Confirm(OwnerWin, $"确定删除用户 {row?.UserName} 吗？", "用户管理"))
            return;

        var result = await AppHost.Users.DeleteAsync(id.Value);
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

    private long? SelectedId() => _grid.CurrentRow?.DataBoundItem is UserListItem row ? row.Id : null;

    private Form OwnerWin => FindForm() is { } f && !ReferenceEquals(f, this) ? f : this;

    private void ConfigureGrid()
    {
        UiChrome.StyleGrid(_grid);
        _grid.Columns.AddRange(
            Col(nameof(UserListItem.UserName), "用户名", 100),
            Col(nameof(UserListItem.RealName), "姓名", 100),
            Col(nameof(UserListItem.Enabled), "启用", 60),
            Col(nameof(UserListItem.RoleNames), "角色", 200));
    }

    private static DataGridViewTextBoxColumn Col(string prop, string header, int weight) => new()
    {
        DataPropertyName = prop,
        HeaderText = header,
        FillWeight = weight,
        MinimumWidth = 60,
        SortMode = DataGridViewColumnSortMode.NotSortable
    };

}
