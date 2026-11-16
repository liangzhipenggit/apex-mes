using Mes.BLL;
using Mes.Common;

namespace Mes.WinUI;

/// <summary>
/// 危险操作的二次授权。只有授权窗体通过后才会执行 action，因此底层通信必须写在 action 里。
/// </summary>
public static class OperationGuard
{
    public const string ElevationPermission = "mes:auth:elevate";

    public static async Task<bool> RunAsync(
        IWin32Window? owner,
        string operation,
        Func<Task> action,
        string permissionCode = ElevationPermission,
        string? barcode = null)
    {
        using var dialog = new ElevationForm(operation);
        if (dialog.ShowDialog(owner) != DialogResult.OK)
            return false;

        var result = await AppHost.Auth.VerifyElevationAsync(dialog.Account, dialog.Password, permissionCode);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Data))
        {
            UiNotify.Warn(owner as Form, result.Message);
            return false;
        }

        AuditLog.Write($"操作员 {AppSession.UserName} 执行「{operation}」，授权人 {dialog.Account}（{result.Data}）。");
        try
        {
            await action();
            DbActivityLog.Risk(operation, barcode, dialog.Account, result.Data, true, null);
            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, operation);
            DbActivityLog.Risk(operation, barcode, dialog.Account, result.Data, false, ex.Message);
            UiNotify.Error(owner as Form, operation + "执行失败，详细信息已写入日志。");
            return false;
        }
    }
}
