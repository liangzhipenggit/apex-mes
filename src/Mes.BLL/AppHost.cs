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
        }

        Auth = new AuthService();
        WorkOrders = new WorkOrderService();
        Users = new UserService();
        Roles = new RoleService();
        Menus = new MenuService();
        PowerBi = powerBi ?? new PowerBiOptions();
        Label = label ?? new LabelOptions();
    }
}
