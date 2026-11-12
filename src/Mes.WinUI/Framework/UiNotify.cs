namespace Mes.WinUI;

/// <summary>统一的 AntdUI 消息与对话框封装。</summary>
public static class UiNotify
{
    public static void Info(Form? owner, string message, string? title = null)
    {
        if (owner != null)
            AntdUI.Message.info(owner, message);
        else
            AntdUI.Modal.open(title ?? "提示", message, AntdUI.TType.Info);
    }

    public static void Success(Form? owner, string message)
    {
        if (owner != null)
            AntdUI.Message.success(owner, message);
        else
            AntdUI.Modal.open("成功", message, AntdUI.TType.Success);
    }

    public static void Warn(Form? owner, string message, string? title = null)
    {
        if (owner != null)
            AntdUI.Message.warn(owner, message);
        else
            AntdUI.Modal.open(title ?? "提示", message, AntdUI.TType.Warn);
    }

    public static void Error(Form? owner, string message, string? title = null)
    {
        if (owner != null)
            AntdUI.Message.error(owner, message);
        else
            AntdUI.Modal.open(title ?? "错误", message, AntdUI.TType.Error);
    }

    public static void Alert(Form? owner, string message, string title, AntdUI.TType type = AntdUI.TType.Info)
    {
        if (owner != null)
            AntdUI.Modal.open(owner, title, message, type);
        else
            AntdUI.Modal.open(title, message, type);
    }

    public static bool Confirm(Form? owner, string message, string title)
    {
        var result = owner != null
            ? AntdUI.Modal.open(owner, title, message, AntdUI.TType.Warn)
            : AntdUI.Modal.open(title, message, AntdUI.TType.Warn);
        return result == DialogResult.OK;
    }
}
