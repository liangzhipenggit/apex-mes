using Mes.BLL;
using Mes.Common;
using Mes.Models;

namespace Mes.WinUI;

public class WorkOrderEditForm : AntdUI.Window
{
    private readonly WorkOrder _model;
    private readonly AntdUI.Input _orderNo = new() { PlaceholderText = "请输入工单号" };
    private readonly AntdUI.Input _productCode = new() { PlaceholderText = "产品编码" };
    private readonly AntdUI.Input _productName = new() { PlaceholderText = "产品名称" };
    private readonly AntdUI.Input _planQty = new() { PlaceholderText = "大于 0" };
    private readonly AntdUI.Select _status = new() { List = true, PlaceholderText = "状态" };
    private readonly AntdUI.DatePicker _planStart = new() { AllowClear = true, Format = "yyyy-MM-dd" };
    private readonly AntdUI.DatePicker _planEnd = new() { AllowClear = true, Format = "yyyy-MM-dd" };
    private readonly AntdUI.Input _remark = new()
    {
        Multiline = true,
        AutoScroll = true,
        PlaceholderText = "选填，最多 200 字"
    };
    private readonly AntdUI.Button _save = new()
    {
        Text = "保存",
        Type = AntdUI.TTypeMini.Primary,
        IconSvg = MenuIcons.Action.Save
    };
    private bool _saving;

    public WorkOrderEditForm(WorkOrder model)
    {
        _model = model;
        Text = model.Id == 0 ? "新增工单" : "修改工单";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(580, 680);
        MinimumSize = new Size(580, 680);
        Resizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.PageBack;
        BorderColor = Color.Transparent;

        var header = UiChrome.CreateDialogHeader(
            Text,
            model.Id == 0 ? "填写工单基本信息与计划" : "调整工单内容后保存");

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceMd, UiTheme.SpaceXl, UiTheme.SpaceMd)
        };
        _save.Size = new Size(120, UiTheme.ControlHeight);
        PageToolbar.StyleIcon(_save);
        var cancel = new AntdUI.Button
        {
            Text = "取消",
            Type = AntdUI.TTypeMini.Default,
            Size = new Size(96, UiTheme.ControlHeight)
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
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
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceLg, UiTheme.SpaceXl, UiTheme.SpaceMd),
            AutoScroll = true
        };

        var y = 4;
        FormUi.AddSection(body, "基本信息", ref y);
        FormUi.AddField(body, "工单号", _orderNo, ref y, 400);
        FormUi.AddField(body, "产品编码", _productCode, ref y, 400);
        FormUi.AddField(body, "产品名称", _productName, ref y, 400);
        FormUi.AddField(body, "计划数量", _planQty, ref y, 200);
        FormUi.AddField(body, "状态", _status, ref y, 200);

        FormUi.AddSection(body, "计划时间", ref y);
        FormUi.AddField(body, "计划开始", _planStart, ref y, 220);
        FormUi.AddField(body, "计划结束", _planEnd, ref y, 220);

        FormUi.AddSection(body, "备注", ref y);
        var remarkLabel = FormUi.Caption("说明", 0, 0);
        FormUi.Place(body, remarkLabel, 0, y, FormUi.LabelWidth, FormUi.EditorHeight);
        FormUi.Place(body, _remark, FormUi.EditorLeft, y, 400, 110);
        _remark.MaxLength = 200;

        _orderNo.MaxLength = 32;
        _productCode.MaxLength = 64;
        _productName.MaxLength = 128;
        _status.Items.Add(new AntdUI.SelectItem("草稿", WorkOrderStatus.Draft));
        _status.Items.Add(new AntdUI.SelectItem("已下达", WorkOrderStatus.Released));
        _status.Items.Add(new AntdUI.SelectItem("已关闭", WorkOrderStatus.Closed));

        BindModel();
        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
    }

    private void BindModel()
    {
        _orderNo.Text = _model.OrderNo;
        _productCode.Text = _model.ProductCode;
        _productName.Text = _model.ProductName;
        _planQty.Text = _model.PlanQty > 0 ? _model.PlanQty.ToString("0.##") : string.Empty;
        _status.SelectedValue = _model.Status;
        _status.Text = WorkOrderStatus.NameOf(_model.Status);
        _planStart.Value = _model.PlanStart;
        _planEnd.Value = _model.PlanEnd;
        _remark.Text = _model.Remark ?? string.Empty;
    }

    private async Task SaveAsync()
    {
        if (_saving)
            return;

        if (!decimal.TryParse(_planQty.Text.Trim(), out var qty))
        {
            UiNotify.Warn(this, "计划数量必须是大于 0 的数字。");
            _planQty.Focus();
            return;
        }

        _model.OrderNo = _orderNo.Text.Trim();
        _model.ProductCode = _productCode.Text.Trim();
        _model.ProductName = _productName.Text.Trim();
        _model.PlanQty = qty;
        _model.Status = _status.SelectedValue is int status ? status : WorkOrderStatus.Draft;
        _model.PlanStart = _planStart.Value?.Date;
        _model.PlanEnd = _planEnd.Value?.Date;
        _model.Remark = _remark.Text;

        var error = WorkOrderService.Validate(_model);
        if (error != null)
        {
            UiNotify.Warn(this, error);
            return;
        }

        _saving = true;
        _save.Loading = true;
        _save.Enabled = false;
        try
        {
            var result = await AppHost.WorkOrders.SaveAsync(_model);
            if (!result.Success)
            {
                UiNotify.Warn(this, result.Message);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "保存工单");
            UiNotify.Error(this, "保存工单失败，详细信息已写入本地日志。");
        }
        finally
        {
            _saving = false;
            if (!IsDisposed)
            {
                _save.Loading = false;
                _save.Enabled = true;
            }
        }
    }
}
