namespace Mes.Models;

public class UserQuery
{
    public string? Keyword { get; set; }

    public bool? Enabled { get; set; }

    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public class RoleQuery
{
    public string? Keyword { get; set; }

    public bool? Enabled { get; set; }

    public int PageIndex { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public class MenuQuery
{
    public string? Keyword { get; set; }

    public PermissionType? PermType { get; set; }
}

public class UserSaveModel
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string RealName { get; set; } = string.Empty;

    /// <summary>新增必填；修改留空表示不改密码。</summary>
    public string? Password { get; set; }

    public bool Enabled { get; set; } = true;

    public List<long> RoleIds { get; set; } = [];
}

public class RoleSaveModel
{
    public long Id { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string RoleCode { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public List<long> PermissionIds { get; set; } = [];
}

public class MenuSaveModel
{
    public long Id { get; set; }

    public long ParentId { get; set; }

    public string PermName { get; set; } = string.Empty;

    public string PermCode { get; set; } = string.Empty;

    public PermissionType PermType { get; set; } = PermissionType.Menu;

    public string? FormKey { get; set; }

    public int Sort { get; set; }
}

public class UserListItem
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string RealName { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string RoleNames { get; set; } = string.Empty;

    public List<long> RoleIds { get; set; } = [];
}

public class RoleListItem
{
    public long Id { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string RoleCode { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public int PermissionCount { get; set; }
}
