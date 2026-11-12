namespace Mes.WinUI;

/// <summary>菜单 FormKey / 标题 → AntdUI 内置图标名。</summary>
public static class MenuIcons
{
    private static readonly Dictionary<string, string> ByFormKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Home"] = "HomeOutlined",
        ["Sys.User"] = "UserOutlined",
        ["Sys.Role"] = "TeamOutlined",
        ["Sys.Menu"] = "MenuOutlined",
        ["Sys.Settings"] = "SettingOutlined",
        ["Mes.WorkOrder"] = "ProfileOutlined",
        ["Mes.PowerBi"] = "DashboardOutlined",
        ["Mes.LabelPrint"] = "PrinterOutlined",
        ["Mes.Station"] = "BarcodeOutlined"
    };

    private static readonly Dictionary<string, string> ByTitle = new(StringComparer.OrdinalIgnoreCase)
    {
        ["首页"] = "HomeOutlined",
        ["系统管理"] = "SettingOutlined",
        ["用户管理"] = "UserOutlined",
        ["角色管理"] = "TeamOutlined",
        ["菜单管理"] = "MenuOutlined",
        ["系统设置"] = "ControlOutlined",
        ["生产管理"] = "AppstoreOutlined",
        ["工单管理"] = "ProfileOutlined",
        ["标签打印"] = "PrinterOutlined",
        ["过站防错"] = "BarcodeOutlined",
        ["集成应用"] = "ApartmentOutlined",
        ["生产看板"] = "DashboardOutlined",
        ["Power BI"] = "DashboardOutlined",
        ["报表看板"] = "DashboardOutlined"
    };

    public const string Default = "AppstoreOutlined";

    public static string Resolve(string? formKey, string? title)
    {
        if (!string.IsNullOrWhiteSpace(formKey) && ByFormKey.TryGetValue(formKey, out var byKey))
            return byKey;
        if (!string.IsNullOrWhiteSpace(title) && ByTitle.TryGetValue(title, out var byTitle))
            return byTitle;
        return Default;
    }

    public static class Action
    {
        public const string Search = "SearchOutlined";
        public const string Add = "PlusOutlined";
        public const string Edit = "EditOutlined";
        public const string Delete = "DeleteOutlined";
        public const string Refresh = "ReloadOutlined";
        public const string Save = "SaveOutlined";
        public const string Login = "LoginOutlined";
        public const string Logout = "LogoutOutlined";
        public const string Check = "CheckCircleOutlined";
        public const string Warn = "WarningOutlined";
        public const string Print = "PrinterOutlined";
        public const string Eye = "EyeOutlined";
    }
}
