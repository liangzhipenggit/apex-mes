using System.Diagnostics;
using System.Runtime.InteropServices;
using Mes.Common;
using Mes.Models;

namespace Mes.WinUI;

/// <summary>
/// BarTender Automation。优先复用 COM 引擎（BarTender.Application / btinet）做高速套打；
/// 未注册 COM 时退回 bartend.exe 命令行。模板里要有同名子串 MaterialCode、BatchNo、Barcode。
/// </summary>
public sealed class BarTenderLabelPrinter : IDisposable
{
    private const int DoNotSaveChanges = 1;

    private object? _application;
    private object? _format;
    private string? _openedTemplate;

    public PrintOutcome Print(string templatePath, LabelPrintData data, string? printerName, string? barTenderExe, bool keepEngine = true)
    {
        if (!File.Exists(templatePath))
            return new PrintOutcome(false, "找不到 BarTender 模板：" + templatePath);
        if (data.Copies < 1)
            return new PrintOutcome(false, "打印份数必须大于 0。");

        var progId = Type.GetTypeFromProgID("BarTender.Application");
        if (progId == null)
            return PrintByCommandLine(templatePath, data, printerName, barTenderExe);

        try
        {
            _application ??= Activator.CreateInstance(progId)
                             ?? throw new InvalidOperationException("无法创建 BarTender.Application。");
            dynamic app = _application;
            app.Visible = false;

            if (_format == null || !string.Equals(_openedTemplate, templatePath, StringComparison.OrdinalIgnoreCase))
            {
                CloseFormat();
                _format = app.Formats.Open(templatePath, false, "");
                _openedTemplate = templatePath;
            }

            dynamic format = _format!;
            format.SetNamedSubStringValue("MaterialCode", data.MaterialCode);
            format.SetNamedSubStringValue("BatchNo", data.BatchNo);
            format.SetNamedSubStringValue("Barcode", data.Barcode);
            if (!string.IsNullOrWhiteSpace(printerName))
                format.Printer = printerName;
            format.IdenticalCopiesOfLabel = data.Copies;
            format.PrintOut(false, false);

            if (!keepEngine)
                Dispose();

            return new PrintOutcome(true, "BarTender 已完成套打。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "BarTender COM 打印");
            Dispose();
            return new PrintOutcome(false, "BarTender 打印失败。请确认模板包含命名子串 MaterialCode、BatchNo、Barcode，详细信息已写入日志。");
        }
    }

    public PrintOutcome PrintBatch(string templatePath, IReadOnlyList<LabelPrintData> labels, string? printerName, string? barTenderExe)
    {
        PrintOutcome last = new(false, "没有要打印的标签。");
        foreach (var label in labels)
        {
            last = Print(templatePath, label, printerName, barTenderExe, keepEngine: true);
            if (!last.Success)
                return last;
        }

        return last;
    }

    private static PrintOutcome PrintByCommandLine(string templatePath, LabelPrintData data, string? printerName, string? barTenderExe)
    {
        var exe = ResolveExe(barTenderExe);
        if (exe == null)
            return new PrintOutcome(false, "未检测到 BarTender Automation。请安装 BarTender，或在 appsettings.json 的 Label:BarTenderExe 中填写 bartend.exe 路径。");

        try
        {
            var args = "/F=" + Quote(templatePath)
                       + " /P /X /C=" + data.Copies
                       + " /?MaterialCode=" + Quote(data.MaterialCode)
                       + " /?BatchNo=" + Quote(data.BatchNo)
                       + " /?Barcode=" + Quote(data.Barcode);
            if (!string.IsNullOrWhiteSpace(printerName))
                args += " /PRN=" + Quote(printerName);

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null)
                return new PrintOutcome(false, "无法启动 bartend.exe。");

            process.WaitForExit();
            return process.ExitCode == 0
                ? new PrintOutcome(true, "已通过命令行提交 BarTender 打印。")
                : new PrintOutcome(false, "bartend.exe 返回错误码 " + process.ExitCode + "。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "BarTender 命令行打印");
            return new PrintOutcome(false, "BarTender 命令行打印失败，详细信息已写入日志。");
        }
    }

    private static string? ResolveExe(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return configured;

        string[] candidates =
        [
            @"C:\Program Files\Seagull\BarTender 2022\bartend.exe",
            @"C:\Program Files\Seagull\BarTender 2021\bartend.exe",
            @"C:\Program Files\Seagull\BarTender Suite\bartend.exe"
        ];
        return candidates.FirstOrDefault(File.Exists);
    }

    private static string Quote(string value)
        => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

    private void CloseFormat()
    {
        if (_format == null)
            return;

        try
        {
            dynamic format = _format;
            format.Close(DoNotSaveChanges);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "关闭 BarTender 模板");
        }
        finally
        {
            Release(_format);
            _format = null;
            _openedTemplate = null;
        }
    }

    public void Dispose()
    {
        CloseFormat();
        if (_application == null)
            return;

        try
        {
            dynamic app = _application;
            app.Quit(DoNotSaveChanges);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "退出 BarTender");
        }
        finally
        {
            Release(_application);
            _application = null;
        }
    }

    private static void Release(object? com)
    {
        if (com != null && Marshal.IsComObject(com))
            Marshal.FinalReleaseComObject(com);
    }
}
