using System.ComponentModel;
using Mes.Common;

namespace Mes.WinUI;

public enum PermissionDenyMode
{
    /// <summary>无权按钮不显示。</summary>
    Hidden,

    /// <summary>无权按钮仍占位，但不可点击。</summary>
    Disabled
}

/// <summary>
/// 带权限码的 AntdUI 按钮。窗体 Load 时由 <see cref="PermissionGuard"/> 统一处理。
/// </summary>
public class PermissionButton : AntdUI.Button
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? PermissionCode { get; set; }
}

/// <summary>
/// 在 Load 时扫描按钮权限。普通 Button 把 Tag 设为 perm:权限码，或直接用 PermissionButton。
/// </summary>
public static class PermissionGuard
{
    public const string TagPrefix = "perm:";

    public static void Apply(Control root, PermissionDenyMode mode = PermissionDenyMode.Hidden)
    {
        foreach (var button in Enumerate(root).OfType<AntdUI.Button>())
        {
            var code = ResolveCode(button);
            if (string.IsNullOrWhiteSpace(code))
                continue;

            var allowed = AppSession.HasPermission(code);
            if (mode == PermissionDenyMode.Hidden)
                button.Visible = allowed;
            else
                button.Enabled = allowed;
        }
    }

    public static string? ResolveCode(AntdUI.Button button)
    {
        if (button is PermissionButton permissionButton
            && !string.IsNullOrWhiteSpace(permissionButton.PermissionCode))
        {
            return permissionButton.PermissionCode;
        }

        if (button.Tag is string tag
            && tag.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase)
            && tag.Length > TagPrefix.Length)
        {
            return tag[TagPrefix.Length..];
        }

        return null;
    }

    private static IEnumerable<Control> Enumerate(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Enumerate(child))
                yield return nested;
        }
    }
}

public static class ButtonPermissionExtensions
{
    public static T BindPermission<T>(this T button, string permissionCode)
        where T : AntdUI.Button
    {
        if (button is PermissionButton permissionButton)
            permissionButton.PermissionCode = permissionCode;
        else
            button.Tag = PermissionGuard.TagPrefix + permissionCode;

        button.AccessibleDescription = permissionCode;
        return button;
    }
}

/// <summary>Load 时自动隐藏或禁用无权按钮的窗体基类（用于嵌入 Tabs 的业务页）。</summary>
public class SecureForm : Form
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PermissionDenyMode DenyMode { get; set; } = PermissionDenyMode.Hidden;

    public SecureForm()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.PageBack;
        TechBackdrop.Apply(this);
        BackgroundImageLayout = ImageLayout.Stretch;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        PermissionGuard.Apply(this, DenyMode);
    }
}

/// <summary>Load 时自动隐藏或禁用无权按钮的页面基类。</summary>
public class SecureUserControl : UserControl
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PermissionDenyMode DenyMode { get; set; } = PermissionDenyMode.Hidden;

    public SecureUserControl()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.PageBack;
        TechBackdrop.Apply(this);
        BackgroundImageLayout = ImageLayout.Stretch;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        PermissionGuard.Apply(this, DenyMode);
    }
}
