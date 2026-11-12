using System.Text.Json;
using Mes.Common;
using Mes.Models;

namespace Mes.DAL;

/// <summary>
/// 演示/本地模式持久化（%LocalAppData%\IndustrialMES\LocalDb\rbac.json）。
/// 表结构与 Scripts 中的 T-SQL 对齐，正式库就绪后切 DemoMode=false 即可迁移。
/// </summary>
public static class LocalDbStore
{
    private static readonly object Sync = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static LocalDbSnapshot? _cache;
    private static string? _filePath;

    public static string FilePath
    {
        get
        {
            if (_filePath != null)
                return _filePath;

            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IndustrialMES",
                "LocalDb");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "rbac.json");
            return _filePath;
        }
    }

    public static void EnsureInitialized()
    {
        lock (Sync)
        {
            _ = LoadUnlocked();
        }
    }

    public static LocalDbSnapshot GetSnapshot()
    {
        lock (Sync)
            return Clone(LoadUnlocked());
    }

    public static void Update(Action<LocalDbSnapshot> mutator)
    {
        lock (Sync)
        {
            var db = LoadUnlocked();
            mutator(db);
            SaveUnlocked(db);
            _cache = Clone(db);
        }
    }

    public static T Update<T>(Func<LocalDbSnapshot, T> mutator)
    {
        lock (Sync)
        {
            var db = LoadUnlocked();
            var result = mutator(db);
            SaveUnlocked(db);
            _cache = Clone(db);
            return result;
        }
    }

    private static LocalDbSnapshot LoadUnlocked()
    {
        if (_cache != null)
            return Clone(_cache);

        if (!File.Exists(FilePath))
        {
            var seeded = CreateSeed();
            SaveUnlocked(seeded);
            _cache = Clone(seeded);
            return Clone(seeded);
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var loaded = JsonSerializer.Deserialize<LocalDbSnapshot>(json, JsonOptions) ?? CreateSeed();
            EnsureSeedMenus(loaded);
            SaveUnlocked(loaded);
            _cache = Clone(loaded);
            return Clone(loaded);
        }
        catch
        {
            var seeded = CreateSeed();
            SaveUnlocked(seeded);
            _cache = Clone(seeded);
            return Clone(seeded);
        }
    }

    private static void SaveUnlocked(LocalDbSnapshot db)
    {
        var json = JsonSerializer.Serialize(db, JsonOptions);
        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Copy(tmp, FilePath, overwrite: true);
        File.Delete(tmp);
    }

    private static LocalDbSnapshot CreateSeed()
    {
        var pwd = PasswordHasher.Hash("123456");
        var db = new LocalDbSnapshot
        {
            Users =
            [
                new SysUser { Id = 1, UserName = "admin", PasswordHash = pwd, RealName = "系统管理员", Enabled = true, CreateTime = DateTime.Now },
                new SysUser { Id = 2, UserName = "operator", PasswordHash = pwd, RealName = "操作员", Enabled = true, CreateTime = DateTime.Now },
                new SysUser { Id = 3, UserName = "engineer", PasswordHash = pwd, RealName = "工艺工程师", Enabled = true, CreateTime = DateTime.Now }
            ],
            Roles =
            [
                new SysRole { Id = 1, RoleName = "系统管理员", RoleCode = "admin", Enabled = true, CreateTime = DateTime.Now },
                new SysRole { Id = 2, RoleName = "操作员", RoleCode = "operator", Enabled = true, CreateTime = DateTime.Now }
            ],
            Permissions = CreatePermissionSeed(),
            UserRoles =
            [
                new SysUserRole { Id = 1, UserId = 1, RoleId = 1 },
                new SysUserRole { Id = 2, UserId = 2, RoleId = 2 },
                new SysUserRole { Id = 3, UserId = 3, RoleId = 1 }
            ],
            NextUserId = 4,
            NextRoleId = 3,
            NextPermissionId = 26,
            NextUserRoleId = 4,
            NextRolePermissionId = 1
        };

        foreach (var perm in db.Permissions)
        {
            db.RolePermissions.Add(new SysRolePermission
            {
                Id = db.NextRolePermissionId++,
                RoleId = 1,
                PermissionId = perm.Id
            });
        }

        foreach (var id in new long[] { 5, 6, 7, 14 })
        {
            db.RolePermissions.Add(new SysRolePermission
            {
                Id = db.NextRolePermissionId++,
                RoleId = 2,
                PermissionId = id
            });
        }

        return db;
    }

    private static void EnsureSeedMenus(LocalDbSnapshot db)
    {
        var required = CreatePermissionSeed();
        foreach (var item in required)
        {
            if (db.Permissions.Any(p => p.Id == item.Id || p.PermCode.Equals(item.PermCode, StringComparison.OrdinalIgnoreCase)))
                continue;
            db.Permissions.Add(item);
            if (!db.RolePermissions.Any(rp => rp.RoleId == 1 && rp.PermissionId == item.Id))
            {
                db.RolePermissions.Add(new SysRolePermission
                {
                    Id = db.NextRolePermissionId++,
                    RoleId = 1,
                    PermissionId = item.Id
                });
            }
        }

        if (db.NextPermissionId < 26)
            db.NextPermissionId = 26;
    }

    private static List<SysPermission> CreatePermissionSeed() =>
    [
        new() { Id = 1, ParentId = 0, PermName = "系统管理", PermCode = "sys:system", PermType = PermissionType.Directory, Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 2, ParentId = 1, PermName = "用户管理", PermCode = "sys:user:view", PermType = PermissionType.Menu, FormKey = "Sys.User", Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 3, ParentId = 2, PermName = "新建用户", PermCode = "sys:user:create", PermType = PermissionType.Button, Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 4, ParentId = 2, PermName = "删除用户", PermCode = "sys:user:delete", PermType = PermissionType.Button, Sort = 20, CreateTime = DateTime.Now },
        new() { Id = 24, ParentId = 2, PermName = "修改用户", PermCode = "sys:user:edit", PermType = PermissionType.Button, Sort = 15, CreateTime = DateTime.Now },
        new() { Id = 16, ParentId = 1, PermName = "角色管理", PermCode = "sys:role:view", PermType = PermissionType.Menu, FormKey = "Sys.Role", Sort = 20, CreateTime = DateTime.Now },
        new() { Id = 17, ParentId = 16, PermName = "新建角色", PermCode = "sys:role:create", PermType = PermissionType.Button, Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 18, ParentId = 16, PermName = "修改角色", PermCode = "sys:role:edit", PermType = PermissionType.Button, Sort = 20, CreateTime = DateTime.Now },
        new() { Id = 19, ParentId = 16, PermName = "删除角色", PermCode = "sys:role:delete", PermType = PermissionType.Button, Sort = 30, CreateTime = DateTime.Now },
        new() { Id = 20, ParentId = 1, PermName = "菜单管理", PermCode = "sys:menu:view", PermType = PermissionType.Menu, FormKey = "Sys.Menu", Sort = 30, CreateTime = DateTime.Now },
        new() { Id = 21, ParentId = 20, PermName = "新建菜单", PermCode = "sys:menu:create", PermType = PermissionType.Button, Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 22, ParentId = 20, PermName = "修改菜单", PermCode = "sys:menu:edit", PermType = PermissionType.Button, Sort = 20, CreateTime = DateTime.Now },
        new() { Id = 23, ParentId = 20, PermName = "删除菜单", PermCode = "sys:menu:delete", PermType = PermissionType.Button, Sort = 30, CreateTime = DateTime.Now },
        new() { Id = 25, ParentId = 1, PermName = "系统设置", PermCode = "sys:settings:view", PermType = PermissionType.Menu, FormKey = "Sys.Settings", Sort = 40, CreateTime = DateTime.Now },
        new() { Id = 5, ParentId = 0, PermName = "生产管理", PermCode = "mes:prod", PermType = PermissionType.Directory, Sort = 20, CreateTime = DateTime.Now },
        new() { Id = 6, ParentId = 5, PermName = "工单管理", PermCode = "mes:wo:view", PermType = PermissionType.Menu, FormKey = "Mes.WorkOrder", Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 7, ParentId = 6, PermName = "新建工单", PermCode = "mes:wo:create", PermType = PermissionType.Button, Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 8, ParentId = 6, PermName = "关闭工单", PermCode = "mes:wo:close", PermType = PermissionType.Button, Sort = 20, CreateTime = DateTime.Now },
        new() { Id = 9, ParentId = 6, PermName = "修改工单", PermCode = "mes:wo:edit", PermType = PermissionType.Button, Sort = 30, CreateTime = DateTime.Now },
        new() { Id = 10, ParentId = 6, PermName = "删除工单", PermCode = "mes:wo:delete", PermType = PermissionType.Button, Sort = 40, CreateTime = DateTime.Now },
        new() { Id = 14, ParentId = 5, PermName = "过站防错", PermCode = "mes:station:view", PermType = PermissionType.Menu, FormKey = "Mes.Station", Sort = 20, CreateTime = DateTime.Now },
        new() { Id = 15, ParentId = 14, PermName = "临时授权", PermCode = "mes:auth:elevate", PermType = PermissionType.Button, Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 11, ParentId = 0, PermName = "集成应用", PermCode = "mes:report", PermType = PermissionType.Directory, Sort = 30, CreateTime = DateTime.Now },
        new() { Id = 12, ParentId = 11, PermName = "生产看板", PermCode = "mes:report:view", PermType = PermissionType.Menu, FormKey = "Mes.PowerBi", Sort = 10, CreateTime = DateTime.Now },
        new() { Id = 13, ParentId = 11, PermName = "标签打印", PermCode = "mes:label:print", PermType = PermissionType.Menu, FormKey = "Mes.LabelPrint", Sort = 20, CreateTime = DateTime.Now }
    ];

    private static LocalDbSnapshot Clone(LocalDbSnapshot src)
    {
        var json = JsonSerializer.Serialize(src, JsonOptions);
        return JsonSerializer.Deserialize<LocalDbSnapshot>(json, JsonOptions)!;
    }
}

public sealed class LocalDbSnapshot
{
    public List<SysUser> Users { get; set; } = [];

    public List<SysRole> Roles { get; set; } = [];

    public List<SysPermission> Permissions { get; set; } = [];

    public List<SysUserRole> UserRoles { get; set; } = [];

    public List<SysRolePermission> RolePermissions { get; set; } = [];

    public long NextUserId { get; set; } = 1;

    public long NextRoleId { get; set; } = 1;

    public long NextPermissionId { get; set; } = 1;

    public long NextUserRoleId { get; set; } = 1;

    public long NextRolePermissionId { get; set; } = 1;
}
