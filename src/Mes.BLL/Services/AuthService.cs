using Mes.Common;
using Mes.DAL;
using Mes.Models;

namespace Mes.BLL;

public class AuthService
{
    private readonly RbacService _rbac = new();

    public async Task<ApiResult<LoginResult>> LoginAsync(string userName, string password, bool demoMode)
    {
        userName = userName.Trim();
        if (userName.Length == 0 || password.Length == 0)
            return ApiResult<LoginResult>.Fail("请输入用户名和密码。");

        if (demoMode)
            return LocalLogin(userName, password);

        try
        {
            return await DbLoginAsync(userName, password);
        }
        catch (Exception ex)
        {
            return ApiResult<LoginResult>.Fail("无法连接数据库：" + ex.Message);
        }
    }

    private static ApiResult<LoginResult> LocalLogin(string userName, string password)
    {
        LocalDbStore.EnsureInitialized();
        var db = LocalDbStore.GetSnapshot();
        var user = db.Users.FirstOrDefault(u =>
            !u.IsDeleted && u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        if (user == null
            || !string.Equals(user.PasswordHash, PasswordHasher.Hash(password), StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<LoginResult>.Fail("用户名或密码错误。");
        }

        if (!user.Enabled)
            return ApiResult<LoginResult>.Fail("账号已停用。");

        var (codes, menus) = RbacHelper.LoadFromLocal(user.Id);
        AppSession.SignIn(user.Id, user.UserName, user.RealName, codes, menus);
        return ApiResult<LoginResult>.Ok(new LoginResult
        {
            UserId = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Permissions = codes,
            Menus = menus
        });
    }

    private async Task<ApiResult<LoginResult>> DbLoginAsync(string userName, string password)
    {
        var users = await SqlSugarSetup.Db.Queryable<SysUser>()
            .Where(u => u.UserName == userName && u.IsDeleted == false)
            .Take(1)
            .ToListAsync();
        var user = users.FirstOrDefault();
        if (user == null
            || !string.Equals(user.PasswordHash, PasswordHasher.Hash(password), StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<LoginResult>.Fail("用户名或密码错误。");
        }

        if (!user.Enabled)
            return ApiResult<LoginResult>.Fail("账号已停用。");

        var (codes, menus) = await _rbac.LoadForUserAsync(user.Id);
        AppSession.SignIn(user.Id, user.UserName, user.RealName, codes, menus);
        return ApiResult<LoginResult>.Ok(new LoginResult
        {
            UserId = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Permissions = codes,
            Menus = menus
        });
    }

    /// <summary>
    /// 临时授权。不改变当前登录会话，只确认这组账号具备指定权限。
    /// </summary>
    public async Task<ApiResult<string>> VerifyElevationAsync(string userName, string password, string permissionCode)
    {
        userName = userName.Trim();
        if (userName.Length == 0 || password.Length == 0)
            return ApiResult<string>.Fail("请输入授权账号和密码。");

        try
        {
            if (AppHost.DemoMode)
            {
                var db = LocalDbStore.GetSnapshot();
                var user = db.Users.FirstOrDefault(u =>
                    !u.IsDeleted && u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
                if (user == null
                    || !string.Equals(user.PasswordHash, PasswordHasher.Hash(password), StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResult<string>.Fail("授权账号或密码错误。");
                }

                if (!user.Enabled)
                    return ApiResult<string>.Fail("授权账号已停用。");

                var (codes, _) = RbacHelper.LoadFromLocal(user.Id);
                if (!codes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
                    return ApiResult<string>.Fail("该账号没有临时授权权限。");

                return ApiResult<string>.Ok(user.RealName);
            }

            var users = await SqlSugarSetup.Db.Queryable<SysUser>()
                .Where(u => u.UserName == userName && u.IsDeleted == false)
                .Take(1)
                .ToListAsync();
            var dbUser = users.FirstOrDefault();
            if (dbUser == null
                || !string.Equals(dbUser.PasswordHash, PasswordHasher.Hash(password), StringComparison.OrdinalIgnoreCase))
            {
                return ApiResult<string>.Fail("授权账号或密码错误。");
            }

            if (!dbUser.Enabled)
                return ApiResult<string>.Fail("授权账号已停用。");

            var (dbCodes, _) = await _rbac.LoadForUserAsync(dbUser.Id);
            if (!dbCodes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase))
                return ApiResult<string>.Fail("该账号没有临时授权权限。");

            return ApiResult<string>.Ok(dbUser.RealName);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "临时授权");
            return ApiResult<string>.Fail("授权校验失败，详细信息已写入本地日志。");
        }
    }
}
