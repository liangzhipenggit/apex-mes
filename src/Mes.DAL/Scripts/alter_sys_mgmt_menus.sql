/*
    增量脚本：为已有库补充「角色管理 / 菜单管理 / 修改用户」权限节点。
    全新库请直接执行 init.sql，无需再跑本脚本。
*/

SET IDENTITY_INSERT Sys_Permission ON;

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 24)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (24, 2, N'修改用户', N'sys:user:edit', 3, NULL, 15);

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 16)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (16, 1, N'角色管理', N'sys:role:view', 2, N'Sys.Role', 20);

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 17)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (17, 16, N'新建角色', N'sys:role:create', 3, NULL, 10);

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 18)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (18, 16, N'修改角色', N'sys:role:edit', 3, NULL, 20);

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 19)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (19, 16, N'删除角色', N'sys:role:delete', 3, NULL, 30);

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 20)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (20, 1, N'菜单管理', N'sys:menu:view', 2, N'Sys.Menu', 30);

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 21)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (21, 20, N'新建菜单', N'sys:menu:create', 3, NULL, 10);

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 22)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (22, 20, N'修改菜单', N'sys:menu:edit', 3, NULL, 20);

IF NOT EXISTS (SELECT 1 FROM Sys_Permission WHERE Id = 23)
    INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort)
    VALUES (23, 20, N'删除菜单', N'sys:menu:delete', 3, NULL, 30);

SET IDENTITY_INSERT Sys_Permission OFF;

-- 管理员角色补授权
INSERT INTO Sys_RolePermission (RoleId, PermissionId)
SELECT 1, p.Id
FROM Sys_Permission p
WHERE p.Id IN (16, 17, 18, 19, 20, 21, 22, 23, 24)
  AND NOT EXISTS (
      SELECT 1 FROM Sys_RolePermission rp
      WHERE rp.RoleId = 1 AND rp.PermissionId = p.Id
  );
