using System.Globalization;
using Mes.Common;
using Mes.DAL;
using Mes.Models;

namespace Mes.BLL;

/// <summary>
/// 把现场操作写入 SQL Server。演示模式或数据库不可用时直接返回，调用方仍保留本地文件。
/// </summary>
public static class DbActivityLog
{
    public static void Audit(string message)
    {
        if (Skip())
            return;

        SqlSugarSetup.Db.Insertable(new SysAuditLog
        {
            Message = Clip(message, 1000),
            CreateTime = DateTime.Now,
            CreateUserId = UserId()
        }).ExecuteCommand();
    }

    public static void Error(Exception exception, string action)
    {
        if (Skip())
            return;

        SqlSugarSetup.Db.Insertable(new SysErrorLog
        {
            Action = Clip(action, 128),
            Detail = exception.ToString(),
            CreateTime = DateTime.Now,
            CreateUserId = UserId()
        }).ExecuteCommand();
    }

    public static void Scan(string barcode, string source, string? portName, bool passed, string? message)
    {
        if (Skip() || string.IsNullOrWhiteSpace(barcode))
            return;

        try
        {
            SqlSugarSetup.Db.Insertable(new ScanRecord
            {
                Barcode = Clip(barcode.Trim(), 128),
                Source = Clip(source, 32),
                PortName = string.IsNullOrWhiteSpace(portName) ? null : Clip(portName.Trim(), 32),
                Passed = passed,
                Message = ClipOrNull(message, 500),
                CreateTime = DateTime.Now,
                CreateUserId = UserId()
            }).ExecuteCommand();
        }
        catch
        {
            // 扫码结果已经展示，落库失败不打断过站。
        }
    }

    public static void Risk(
        string operation,
        string? barcode,
        string authorizerName,
        string? authorizerRealName,
        bool success,
        string? message)
    {
        if (Skip() || string.IsNullOrWhiteSpace(authorizerName))
            return;

        try
        {
            SqlSugarSetup.Db.Insertable(new RiskOperation
            {
                Operation = Clip(operation, 64),
                Barcode = string.IsNullOrWhiteSpace(barcode) ? null : Clip(barcode.Trim(), 128),
                OperatorName = ClipOrNull(AppSession.UserName, 64),
                AuthorizerName = Clip(authorizerName.Trim(), 64),
                AuthorizerRealName = ClipOrNull(authorizerRealName, 64),
                Success = success,
                Message = ClipOrNull(message, 500),
                CreateTime = DateTime.Now,
                CreateUserId = UserId()
            }).ExecuteCommand();
        }
        catch
        {
            // 授权动作本身已经结束。
        }
    }

    public static void Label(LabelPrintLog entry)
    {
        if (Skip())
            return;

        try
        {
            entry.MaterialCode = Clip(entry.MaterialCode, 64);
            entry.BatchNo = ClipOrNull(entry.BatchNo, 64);
            entry.Barcode = Clip(entry.Barcode, 128);
            entry.Engine = Clip(entry.Engine, 32);
            entry.TemplatePath = ClipOrNull(entry.TemplatePath, 260);
            entry.PrinterName = ClipOrNull(entry.PrinterName, 128);
            entry.Message = ClipOrNull(entry.Message, 500);
            if (entry.Copies < 1)
                entry.Copies = 1;
            entry.CreateTime = DateTime.Now;
            entry.CreateUserId = UserId();
            SqlSugarSetup.Db.Insertable(entry).ExecuteCommand();
        }
        catch
        {
            // 打印结果已经返回给界面。
        }
    }

    public static void SaveUi(string fontFamily, float fontSize, bool fontBold)
    {
        if (Skip())
            return;

        try
        {
            Upsert("Ui.FontFamily", fontFamily, "业务页字体");
            Upsert("Ui.FontSize", fontSize.ToString("0.0", CultureInfo.InvariantCulture), "业务页字号");
            Upsert("Ui.FontBold", fontBold ? "1" : "0", "业务页是否加粗，0 否 1 是");
        }
        catch
        {
            // 本地 ui-settings.json 已经保存。
        }
    }

    private static void Upsert(string key, string value, string remark)
    {
        var db = SqlSugarSetup.Db;
        var existing = db.Queryable<SysSetting>()
            .Where(s => s.SettingKey == key)
            .Take(1)
            .ToList()
            .FirstOrDefault();
        if (existing == null)
        {
            db.Insertable(new SysSetting
            {
                SettingKey = key,
                SettingValue = Clip(value, 500),
                Remark = remark,
                CreateTime = DateTime.Now,
                CreateUserId = UserId()
            }).ExecuteCommand();
            return;
        }

        existing.SettingValue = Clip(value, 500);
        existing.UpdateTime = DateTime.Now;
        existing.UpdateUserId = UserId();
        db.Updateable(existing).ExecuteCommand();
    }

    private static bool Skip() => AppHost.DemoMode;

    private static long? UserId()
        => AppSession.IsAuthenticated ? AppSession.UserId : null;

    private static string Clip(string? value, int max)
    {
        var text = value ?? string.Empty;
        return text.Length <= max ? text : text[..max];
    }

    private static string? ClipOrNull(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim();
        return text.Length <= max ? text : text[..max];
    }
}
