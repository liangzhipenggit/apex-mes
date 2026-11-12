using System.Reflection;
using Mes.Common;
using Mes.Models;

namespace Mes.WinUI;

/// <summary>
/// FastReport.Net 动态传参。模板中需定义参数 MaterialCode、BatchNo、Barcode。
/// 运行时加载 FastReport.dll（程序目录或已安装的程序集），不把商业库写进编译引用。
/// </summary>
public static class FastReportLabelPrinter
{
    public static PrintOutcome Preview(string templatePath, LabelPrintData data, IWin32Window? owner)
        => Run(templatePath, data, printerName: null, preview: true, owner);

    public static PrintOutcome PrintSilent(string templatePath, LabelPrintData data, string? printerName)
        => Run(templatePath, data, printerName, preview: false, owner: null);

    private static PrintOutcome Run(
        string templatePath,
        LabelPrintData data,
        string? printerName,
        bool preview,
        IWin32Window? owner)
    {
        if (!File.Exists(templatePath))
            return new PrintOutcome(false, "找不到 FastReport 模板：" + templatePath);

        try
        {
            var assembly = LoadAssembly();
            var reportType = assembly.GetType("FastReport.Report")
                             ?? throw new InvalidOperationException("FastReport.dll 中没有 FastReport.Report。");
            dynamic report = Activator.CreateInstance(reportType)!;
            try
            {
                report.Load(templatePath);
                report.SetParameterValue("MaterialCode", data.MaterialCode);
                report.SetParameterValue("BatchNo", data.BatchNo);
                report.SetParameterValue("Barcode", data.Barcode);
                var prepared = (bool)report.Prepare();
                if (!prepared)
                    return new PrintOutcome(false, "报表准备失败。请确认模板参数名为 MaterialCode、BatchNo、Barcode。");

                if (preview)
                {
                    report.ShowPrepared(owner, true);
                    return new PrintOutcome(true, "已打开打印预览。");
                }

                report.PrintSettings.ShowDialog = false;
                if (!string.IsNullOrWhiteSpace(printerName))
                    report.PrintSettings.Printer = printerName;
                report.Print();
                return new PrintOutcome(true, "已发送到打印机。");
            }
            finally
            {
                report.Dispose();
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "FastReport 标签打印");
            return new PrintOutcome(false, "FastReport 打印失败。请确认已安装 FastReport.Net，详细信息已写入本地日志。");
        }
    }

    private static Assembly LoadAssembly()
    {
        var local = Path.Combine(AppContext.BaseDirectory, "FastReport.dll");
        return File.Exists(local) ? Assembly.LoadFrom(local) : Assembly.Load("FastReport");
    }
}
