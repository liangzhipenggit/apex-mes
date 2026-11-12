using Mes.Models;

namespace Mes.Common;

/// <summary>
/// 当前客户端登录上下文。WinForms 单用户进程内使用。
/// </summary>
public static class AppSession
{
    private static List<string> _permissions = [];

    public static long UserId { get; private set; }

    public static string UserName { get; private set; } = string.Empty;

    public static string RealName { get; private set; } = string.Empty;

    public static IReadOnlyList<string> Permissions => _permissions;

    /// <summary>
    /// 登录时按权限裁剪好的菜单树，主界面只读这份数据。
    /// </summary>
    public static IReadOnlyList<MenuNodeDto> Menus { get; private set; } = [];

    public static bool IsAuthenticated => UserId > 0;

    public static void SignIn(
        long userId,
        string userName,
        string realName,
        IEnumerable<string>? permissions,
        IEnumerable<MenuNodeDto>? menus = null)
    {
        UserId = userId;
        UserName = userName ?? string.Empty;
        RealName = realName ?? string.Empty;
        _permissions = permissions?
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        Menus = menus?.ToList() ?? [];
    }

    public static void SignOut()
    {
        UserId = 0;
        UserName = string.Empty;
        RealName = string.Empty;
        _permissions = [];
        Menus = [];
    }

    public static bool HasPermission(string? code)
        => !string.IsNullOrWhiteSpace(code)
           && _permissions.Contains(code, StringComparer.OrdinalIgnoreCase);

    public static bool HasAny(params string[] codes)
        => codes.Any(HasPermission);
}
