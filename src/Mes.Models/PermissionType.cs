namespace Mes.Models;

/// <summary>
/// 权限节点类型。目录只用于菜单分组，菜单打开功能页，按钮控制页内操作。
/// </summary>
public enum PermissionType
{
    Directory = 1,
    Menu = 2,
    Button = 3
}
