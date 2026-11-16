using Mes.Common;
using Mes.DAL;
using Mes.Models;

namespace Mes.BLL;

public static class AppHost
{
    public static bool DemoMode { get; private set; }

    public static AuthService Auth { get; private set; } = null!;

    public static WorkOrderService WorkOrders { get; private set; } = null!;

    public static UserService Users { get; private set; } = null!;

    public static RoleService Roles { get; private set; } = null!;

    public static MenuService Menus { get; private set; } = null!;

    public static PowerBiOptions PowerBi { get; private set; } = new();

    public static LabelOptions Label { get; private set; } = new();

    /// <summary>当前连接的库，例如 IndustrialMES @ 127.0.0.1,14330。</summary>
    public static string DatabaseTarget { get; private set; } = string.Empty;

    /// <summary>启动时连不上数据库的原因。为空表示已连上，或仍在演示模式。</summary>
    public static string? DatabaseError { get; private set; }

    public static void Initialize(string connectionString, bool demoMode, PowerBiOptions? powerBi = null, LabelOptions? label = null)
    {
        DemoMode = demoMode;
        if (demoMode)
        {
            LocalDbStore.EnsureInitialized();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(connectionString)
                || connectionString.Contains("YourPassword", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("已关闭演示模式，请在 appsettings.json 中填写真实的 SQL Server 连接串。");
            }

            SqlSugarSetup.Init(connectionString);
            DatabaseTarget = Describe(connectionString);
            AuditLog.Persist = DbActivityLog.Audit;
            ErrorLog.Persist = DbActivityLog.Error;
            try
            {
                SqlSugarSetup.EnsureConnected();
                DatabaseError = null;
            }
            catch (Exception ex)
            {
                DatabaseError = ex.GetBaseException().Message;
            }
        }

        Auth = new AuthService();
        WorkOrders = new WorkOrderService();
        Users = new UserService();
        Roles = new RoleService();
        Menus = new MenuService();
        PowerBi = powerBi ?? new PowerBiOptions();
        Label = label ?? new LabelOptions();
    }

    private static string Describe(string connectionString)
    {
        var server = "";
        var database = "";
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var index = part.IndexOf('=');
            if (index <= 0)
                continue;
            var key = part[..index].Trim();
            var value = part[(index + 1)..].Trim();
            if (key.Equals("Server", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                server = value;
            else if (key.Equals("Database", StringComparison.OrdinalIgnoreCase)
                     || key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
                database = value;
        }

        if (server.Length == 0)
            return database;
        if (database.Length == 0)
            return server;
        return database + " @ " + server;
    }
}
