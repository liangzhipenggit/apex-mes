using System.ComponentModel;
using Mes.BLL;
using Mes.Common;
using Mes.Models;

namespace Mes.WinUI;

public class WorkOrderForm : SecureForm
{
    private const int PageSize = 20;

    private readonly AntdUI.Input _orderNo = new() { PlaceholderText = "工单号", AllowClear = true };
    private readonly AntdUI.Select _status = new() { PlaceholderText = "状态", List = true };
    private readonly DataGridView _grid = new();
    private readonly AntdUI.Pagination _pager = new()
    {
        PageSize = PageSize,
        ShowSizeChanger = false,
        Dock = DockStyle.Fill
    };
    private readonly BindingList<WorkOrderRow> _rows = [];

    private int _page = 1;
    private int _total;
    private bool _busy;

    public WorkOrderForm()
    {
        Text = "工单管理";
        BackColor = UiTheme.PageBack;

        var toolbar = new PageToolbar();
        var orderLabel = FormUi.Caption("工单号", 0, 16, 56);
        orderLabel.TextAlign = ContentAlignment.MiddleLeft;
        _orderNo.SetBounds(64, 16, 160, UiTheme.ControlHeight);
        ScanCapture.Bind(_orderNo, async code =>
        {
            _orderNo.Text = code;
            _page = 1;
            await ReloadAsync();
        }, new ScanCaptureOptions { ClearAfterRead = false });

        var statusLabel = FormUi.Caption("状态", 240, 16, 40);
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _status.SetBounds(284, 16, 120, UiTheme.ControlHeight);
        var search = new AntdUI.Button
        {
            Text = "查询",
            Type = AntdUI.TTypeMini.Primary,
            Bounds = new Rectangle(420, 16, 108, UiTheme.ControlHeight)
        };
        PageToolbar.StyleIcon(search, MenuIcons.Action.Search);
        search.Click += async (_, _) =>
        {
            _page = 1;
            await ReloadAsync();
        };
        toolbar.LeftHost.Controls.AddRange(orderLabel, _orderNo, statusLabel, _status, search);

        var add = PageToolbar.ActionButton("新增", "mes:wo:create", AntdUI.TTypeMini.Primary, iconSvg: MenuIcons.Action.Add);
        var edit = PageToolbar.ActionButton("修改", "mes:wo:edit", AntdUI.TTypeMini.Default, iconSvg: MenuIcons.Action.Edit);
        var delete = PageToolbar.ActionButton("删除", "mes:wo:delete", AntdUI.TTypeMini.Error, iconSvg: MenuIcons.Action.Delete);
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
            if (_busy || e.Current == _page)
                return;
            _page = Math.Max(1, e.Current);
            await ReloadAsync();
        };

        var gridHost = UiChrome.CreateCard();
        ConfigureGrid();
        gridHost.Controls.Add(_grid);

        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex < 0 || !AppSession.HasPermission("mes:wo:edit"))
                return;
            await EditAsync(SelectedId());
        };

        Controls.Add(gridHost);
        Controls.Add(pagerBar);
        Controls.Add(toolbar);

        _status.Items.Add(new AntdUI.SelectItem("全部", -1));
        _status.Items.Add(new AntdUI.SelectItem("草稿", WorkOrderStatus.Draft));
        _status.Items.Add(new AntdUI.SelectItem("已下达", WorkOrderStatus.Released));
        _status.Items.Add(new AntdUI.SelectItem("已关闭", WorkOrderStatus.Closed));
        _status.SelectedValue = -1;
        _status.Text = "全部";
        _grid.DataSource = _rows;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_busy)
            return;

        _busy = true;
        UseWaitCursor = true;
        try
        {
            while (!IsDisposed)
            {
                int? status = _status.SelectedValue switch
                {
                    int v when v >= 0 => v,
                    _ => null
                };
                var result = await AppHost.WorkOrders.QueryAsync(new WorkOrderQuery
                {
                    OrderNo = _orderNo.Text,
                    Status = status,
                    PageIndex = _page,
                    PageSize = PageSize
                });
                if (IsDisposed)
                    return;

                if (!result.Success || result.Data == null)
                {
                    UiNotify.Warn(OwnerForm as Form ?? this, result.Message);
                    return;
                }

                _total = result.Data.Total;
                if (_page > PageCount)
                {
                    _page = PageCount;
                    continue;
                }

                _rows.RaiseListChangedEvents = false;
                _rows.Clear();
                foreach (var item in result.Data.Items)
                    _rows.Add(WorkOrderRow.From(item));
                _rows.RaiseListChangedEvents = true;
                _rows.ResetBindings();
                break;
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "查询工单");
            UiNotify.Error(OwnerForm as Form ?? this, "查询工单失败，详细信息已写入本地日志。");
        }
        finally
        {
            _busy = false;
            UseWaitCursor = false;
            UpdatePager();
        }
    }

    private async Task EditAsync(long? id)
    {
        if (_busy)
            return;

        try
        {
            WorkOrder model;
            if (id == null)
            {
                model = new WorkOrder { Status = WorkOrderStatus.Draft };
            }
            else
            {
                var loaded = await AppHost.WorkOrders.GetAsync(id.Value);
                if (!loaded.Success || loaded.Data == null)
                {
                    UiNotify.Warn(OwnerForm as Form ?? this, loaded.Message);
                    return;
                }

                model = loaded.Data;
            }

            using var dialog = new WorkOrderEditForm(model);
            if (dialog.ShowDialog(OwnerForm) == DialogResult.OK)
                await ReloadAsync();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, id == null ? "新增工单" : "修改工单");
            UiNotify.Error(OwnerForm as Form ?? this, "打开工单编辑窗口失败，详细信息已写入本地日志。");
        }
    }

    private async Task DeleteAsync()
    {
        var id = SelectedId();
        if (id == null)
        {
            UiNotify.Info(OwnerForm as Form ?? this, "请选择要删除的工单。");
            return;
        }

        var row = _grid.CurrentRow?.DataBoundItem as WorkOrderRow;
        if (!UiNotify.Confirm(OwnerForm as Form ?? this, $"确定删除工单 {row?.OrderNo} 吗？", "工单管理"))
            return;

        try
        {
            var result = await AppHost.WorkOrders.DeleteAsync(id.Value);
            if (result.Success)
            {
                UiNotify.Success(OwnerForm as Form ?? this, result.Message);
                await ReloadAsync();
            }
            else
            {
                UiNotify.Warn(OwnerForm as Form ?? this, result.Message);
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "删除工单");
            UiNotify.Error(OwnerForm as Form ?? this, "删除工单失败，详细信息已写入本地日志。");
        }
    }

    private long? SelectedId()
        => _grid.CurrentRow?.DataBoundItem is WorkOrderRow row ? row.Id : null;

    private int PageCount => Math.Max(1, (int)Math.Ceiling(_total / (double)PageSize));

    private void UpdatePager()
    {
        _pager.Total = _total;
        _pager.Current = _page;
        _pager.PageSize = PageSize;
    }

    private IWin32Window OwnerForm
    {
        get
        {
            var owner = FindForm();
            return owner == null || ReferenceEquals(owner, this) ? this : owner;
        }
    }

    private void ConfigureGrid()
    {
        UiChrome.StyleGrid(_grid);
        _grid.Columns.AddRange(
            Column(nameof(WorkOrderRow.OrderNo), "工单号", 120),
            Column(nameof(WorkOrderRow.ProductCode), "产品编码", 110),
            Column(nameof(WorkOrderRow.ProductName), "产品名称", 160),
            Column(nameof(WorkOrderRow.PlanQty), "计划数量", 80),
            Column(nameof(WorkOrderRow.StatusName), "状态", 80),
            Column(nameof(WorkOrderRow.PlanStart), "计划开始", 100),
            Column(nameof(WorkOrderRow.PlanEnd), "计划结束", 100),
            Column(nameof(WorkOrderRow.Remark), "备注", 160));
    }

    private static DataGridViewTextBoxColumn Column(string property, string header, int weight)
        => new()
        {
            DataPropertyName = property,
            HeaderText = header,
            FillWeight = weight,
            MinimumWidth = 72,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

}

internal sealed class WorkOrderRow
{
    public long Id { get; init; }

    public string OrderNo { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public decimal PlanQty { get; init; }

    public string StatusName { get; init; } = string.Empty;

    public string PlanStart { get; init; } = string.Empty;

    public string PlanEnd { get; init; } = string.Empty;

    public string Remark { get; init; } = string.Empty;

    public static WorkOrderRow From(WorkOrder order) => new()
    {
        Id = order.Id,
        OrderNo = order.OrderNo,
        ProductCode = order.ProductCode,
        ProductName = order.ProductName,
        PlanQty = order.PlanQty,
        StatusName = WorkOrderStatus.NameOf(order.Status),
        PlanStart = order.PlanStart?.ToString("yyyy-MM-dd") ?? string.Empty,
        PlanEnd = order.PlanEnd?.ToString("yyyy-MM-dd") ?? string.Empty,
        Remark = order.Remark ?? string.Empty
    };
}
