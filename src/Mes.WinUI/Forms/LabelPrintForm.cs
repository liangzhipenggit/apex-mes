using Mes.BLL;
using Mes.Models;

namespace Mes.WinUI;

public class LabelPrintForm : Form
{
    private readonly AntdUI.Input _material = new();
    private readonly AntdUI.Input _batch = new();
    private readonly AntdUI.Input _barcode = new();
    private readonly AntdUI.InputNumber _copies = new()
    {
        Minimum = 1,
        Maximum = 999,
        Value = 1
    };
    private readonly BarTenderLabelPrinter _barTender = new();

    public LabelPrintForm()
    {
        Text = "标签打印";
        BackColor = UiTheme.PageBack;

        var root = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiTheme.SpaceLg),
            BackColor = UiTheme.PageBack
        };

        // 不用带 Shadow 的 AntdUI 卡片做绝对定位表单，避免标签落在阴影带被裁切
        var formCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 360,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceLg, UiTheme.SpaceXl, UiTheme.SpaceLg),
            Margin = new Padding(0, 0, 0, UiTheme.SpaceMd)
        };

        var y = 0;
        FormUi.AddSection(formCard, "打印参数", ref y);
        FormUi.AddField(formCard, "物料编码", _material, ref y, 360);
        FormUi.AddField(formCard, "批次", _batch, ref y, 360);
        FormUi.AddField(formCard, "条码", _barcode, ref y, 360);
        FormUi.AddField(formCard, "份数", _copies, ref y, 160);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        var preview = new AntdUI.Button
        {
            Text = "FastReport 预览",
            Type = AntdUI.TTypeMini.Primary,
            Size = new Size(168, UiTheme.ControlHeight),
            Margin = new Padding(0, 0, UiTheme.SpaceSm, 0)
        };
        var silent = new AntdUI.Button
        {
            Text = "FastReport 静默打印",
            Type = AntdUI.TTypeMini.Default,
            Size = new Size(188, UiTheme.ControlHeight),
            Margin = new Padding(0, 0, UiTheme.SpaceSm, 0)
        };
        var bartender = new AntdUI.Button
        {
            Text = "BarTender 套打",
            Type = AntdUI.TTypeMini.Default,
            Size = new Size(168, UiTheme.ControlHeight),
            Margin = new Padding(0)
        };
        PageToolbar.StyleIcon(preview, MenuIcons.Action.Eye);
        PageToolbar.StyleIcon(silent, MenuIcons.Action.Print);
        PageToolbar.StyleIcon(bartender, MenuIcons.Action.Print);
        preview.Click += (_, _) => RunFastReport(preview: true);
        silent.Click += (_, _) => RunFastReport(preview: false);
        bartender.Click += (_, _) => RunBarTender();
        actions.Controls.AddRange(preview, silent, bartender);
        FormUi.Place(formCard, actions, FormUi.EditorLeft, y + 4, 560, UiTheme.ControlHeight + 8);

        var hintCard = UiChrome.CreateCard(DockStyle.Fill, shadow: 4);
        hintCard.Margin = new Padding(0);
        var hint = new AntdUI.Alert
        {
            Dock = DockStyle.Fill,
            Icon = AntdUI.TType.Info,
            CloseIcon = false,
            TextTitle = "模板说明",
            Text = "模板参数名固定为 MaterialCode、BatchNo、Barcode。FastReport 需要程序目录中的 FastReport.dll；BarTender 优先走 COM 引擎连续套打，未安装 Automation 时改走 bartend.exe。"
        };
        hintCard.Controls.Add(hint);

        root.Controls.Add(hintCard);
        root.Controls.Add(formCard);
        Controls.Add(root);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _barTender.Dispose();
        base.Dispose(disposing);
    }

    private LabelPrintData ReadData() => new()
    {
        MaterialCode = _material.Text.Trim(),
        BatchNo = _batch.Text.Trim(),
        Barcode = _barcode.Text.Trim(),
        Copies = (int)_copies.Value
    };

    private bool EnsureData(LabelPrintData data)
    {
        if (data.MaterialCode.Length == 0 || data.Barcode.Length == 0)
        {
            UiNotify.Warn(FindForm(), "物料编码和条码不能为空。");
            return false;
        }

        return true;
    }

    private void RunFastReport(bool preview)
    {
        var data = ReadData();
        if (!EnsureData(data))
            return;

        var path = Resolve(AppHost.Label.FastReportTemplate);
        var outcome = preview
            ? FastReportLabelPrinter.Preview(path, data, this)
            : FastReportLabelPrinter.PrintSilent(path, data, AppHost.Label.PrinterName);
        ShowOutcome(outcome);
    }

    private void RunBarTender()
    {
        var data = ReadData();
        if (!EnsureData(data))
            return;

        var path = Resolve(AppHost.Label.BarTenderTemplate);
        var outcome = _barTender.Print(path, data, AppHost.Label.PrinterName, AppHost.Label.BarTenderExe);
        ShowOutcome(outcome);
    }

    private static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }

    private void ShowOutcome(PrintOutcome outcome)
    {
        if (outcome.Success)
            UiNotify.Success(FindForm(), outcome.Message);
        else
            UiNotify.Warn(FindForm(), outcome.Message);
    }
}
