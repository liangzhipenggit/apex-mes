using Mes.BLL;
using Microsoft.Extensions.Configuration;

namespace Mes.WinUI;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        UiTheme.Apply();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            UiNotify.Alert(null, e.Exception.Message, AppBrand.FullName, AntdUI.TType.Error);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var demoMode = !bool.TryParse(config["DemoMode"], out var parsed) || parsed;
        AppHost.Initialize(
            config.GetConnectionString("Default") ?? string.Empty,
            demoMode,
            new PowerBiOptions
            {
                WorkspaceId = config["PowerBI:WorkspaceId"] ?? string.Empty,
                ReportId = config["PowerBI:ReportId"] ?? string.Empty,
                TenantId = config["PowerBI:TenantId"] ?? string.Empty,
                ClientId = config["PowerBI:ClientId"] ?? string.Empty,
                ClientSecret = config["PowerBI:ClientSecret"] ?? string.Empty,
                EmbedUrl = config["PowerBI:EmbedUrl"] ?? string.Empty
            },
            new LabelOptions
            {
                FastReportTemplate = config["Label:FastReportTemplate"] ?? string.Empty,
                BarTenderTemplate = config["Label:BarTenderTemplate"] ?? string.Empty,
                BarTenderExe = config["Label:BarTenderExe"] ?? string.Empty,
                PrinterName = config["Label:PrinterName"] ?? string.Empty
            });
        Application.Run(new LoginForm());
    }
}
