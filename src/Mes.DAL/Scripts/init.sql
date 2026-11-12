/*
    IndustrialMES RBAC + 业务表建表脚本（SQL Server / LocalDB 通用）
    ---------------------------------------------------------------
    使用方式：
      1) 本地演示：DemoMode=true，数据落在 %LocalAppData%\IndustrialMES\LocalDb\rbac.json
      2) 正式库：DemoMode=false，先对本脚本在目标库执行，再配置 ConnectionStrings:Default

    口令字段保存 PasswordHasher.Hash 的十六进制摘要。
    种子账号（口令均为 123456）：
      admin    系统管理员，拥有全部菜单和按钮
      operator 操作员，可看工单、可新建，不能关闭工单，也不能进入系统管理
      engineer 工艺工程师，同管理员角色（含临时授权）
*/

CREATE TABLE Sys_User (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserName      NVARCHAR(64)  NOT NULL,
    PasswordHash  NVARCHAR(128) NOT NULL,
    RealName      NVARCHAR(64)  NOT NULL,
    Enabled       BIT           NOT NULL CONSTRAINT DF_Sys_User_Enabled DEFAULT (1),
    CreateTime    DATETIME2     NOT NULL CONSTRAINT DF_Sys_User_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT        NULL,
    UpdateTime    DATETIME2     NULL,
    UpdateUserId  BIGINT        NULL,
    IsDeleted     BIT           NOT NULL CONSTRAINT DF_Sys_User_IsDeleted DEFAULT (0)
);
CREATE UNIQUE INDEX UX_Sys_User_UserName ON Sys_User(UserName) WHERE IsDeleted = 0;

CREATE TABLE Sys_Role (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleName      NVARCHAR(64) NOT NULL,
    RoleCode      NVARCHAR(64) NOT NULL,
    Enabled       BIT          NOT NULL CONSTRAINT DF_Sys_Role_Enabled DEFAULT (1),
    CreateTime    DATETIME2    NOT NULL CONSTRAINT DF_Sys_Role_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT       NULL,
    UpdateTime    DATETIME2    NULL,
    UpdateUserId  BIGINT       NULL,
    IsDeleted     BIT          NOT NULL CONSTRAINT DF_Sys_Role_IsDeleted DEFAULT (0)
);
CREATE UNIQUE INDEX UX_Sys_Role_RoleCode ON Sys_Role(RoleCode) WHERE IsDeleted = 0;

CREATE TABLE Sys_Permission (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ParentId      BIGINT        NOT NULL CONSTRAINT DF_Sys_Permission_ParentId DEFAULT (0),
    PermName      NVARCHAR(64)  NOT NULL,
    PermCode      NVARCHAR(64)  NOT NULL,
    PermType      INT           NOT NULL, -- 1 目录  2 菜单  3 按钮
    FormKey       NVARCHAR(64)  NULL,
    Sort          INT           NOT NULL CONSTRAINT DF_Sys_Permission_Sort DEFAULT (0),
    CreateTime    DATETIME2     NOT NULL CONSTRAINT DF_Sys_Permission_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT        NULL,
    UpdateTime    DATETIME2     NULL,
    UpdateUserId  BIGINT        NULL,
    IsDeleted     BIT           NOT NULL CONSTRAINT DF_Sys_Permission_IsDeleted DEFAULT (0)
);
CREATE UNIQUE INDEX UX_Sys_Permission_PermCode ON Sys_Permission(PermCode) WHERE IsDeleted = 0;

CREATE TABLE Sys_UserRole (
    Id      BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId  BIGINT NOT NULL,
    RoleId  BIGINT NOT NULL,
    CONSTRAINT UX_Sys_UserRole UNIQUE (UserId, RoleId)
);

CREATE TABLE Sys_RolePermission (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleId        BIGINT NOT NULL,
    PermissionId  BIGINT NOT NULL,
    CONSTRAINT UX_Sys_RolePermission UNIQUE (RoleId, PermissionId)
);

/* 123456 的 SHA-256 */
DECLARE @pwd NVARCHAR(128) = N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92';

INSERT INTO Sys_Role (RoleName, RoleCode, Enabled) VALUES
(N'系统管理员', N'admin', 1),
(N'操作员', N'operator', 1);

INSERT INTO Sys_User (UserName, PasswordHash, RealName, Enabled) VALUES
(N'admin', @pwd, N'系统管理员', 1),
(N'operator', @pwd, N'操作员', 1),
(N'engineer', @pwd, N'工艺工程师', 1);

SET IDENTITY_INSERT Sys_Permission ON;
INSERT INTO Sys_Permission (Id, ParentId, PermName, PermCode, PermType, FormKey, Sort) VALUES
(1, 0, N'系统管理', N'sys:system',    1, NULL,            10),
(2, 1, N'用户管理', N'sys:user:view', 2, N'Sys.User',     10),
(3, 2, N'新建用户', N'sys:user:create', 3, NULL,          10),
(24, 2, N'修改用户', N'sys:user:edit', 3, NULL,           15),
(4, 2, N'删除用户', N'sys:user:delete', 3, NULL,          20),
(16, 1, N'角色管理', N'sys:role:view', 2, N'Sys.Role',    20),
(17, 16, N'新建角色', N'sys:role:create', 3, NULL,         10),
(18, 16, N'修改角色', N'sys:role:edit', 3, NULL,           20),
(19, 16, N'删除角色', N'sys:role:delete', 3, NULL,         30),
(20, 1, N'菜单管理', N'sys:menu:view', 2, N'Sys.Menu',    30),
(21, 20, N'新建菜单', N'sys:menu:create', 3, NULL,         10),
(22, 20, N'修改菜单', N'sys:menu:edit', 3, NULL,           20),
(23, 20, N'删除菜单', N'sys:menu:delete', 3, NULL,         30),
(25, 1, N'系统设置', N'sys:settings:view', 2, N'Sys.Settings', 40),
(5, 0, N'生产管理', N'mes:prod',      1, NULL,            20),
(6, 5, N'工单管理', N'mes:wo:view',   2, N'Mes.WorkOrder', 10),
(7, 6, N'新建工单', N'mes:wo:create', 3, NULL,            10),
(8, 6, N'关闭工单', N'mes:wo:close',  3, NULL,            20),
(9, 6, N'修改工单', N'mes:wo:edit',   3, NULL,            30),
(10, 6, N'删除工单', N'mes:wo:delete', 3, NULL,           40),
(14, 5, N'过站防错', N'mes:station:view', 2, N'Mes.Station', 20),
(15, 14, N'临时授权', N'mes:auth:elevate', 3, NULL, 10),
(11, 0, N'集成应用', N'mes:report',     1, NULL,            30),
(12, 11, N'生产看板', N'mes:report:view', 2, N'Mes.PowerBi', 10),
(13, 11, N'标签打印', N'mes:label:print', 2, N'Mes.LabelPrint', 20);
SET IDENTITY_INSERT Sys_Permission OFF;

INSERT INTO Sys_UserRole (UserId, RoleId) VALUES (1, 1), (2, 2), (3, 1);

INSERT INTO Sys_RolePermission (RoleId, PermissionId)
SELECT 1, Id FROM Sys_Permission;

INSERT INTO Sys_RolePermission (RoleId, PermissionId) VALUES
(2, 5), (2, 6), (2, 7), (2, 14);

CREATE TABLE Mes_WorkOrder (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderNo       NVARCHAR(32)   NOT NULL,
    ProductCode   NVARCHAR(64)   NOT NULL,
    ProductName   NVARCHAR(128)  NOT NULL,
    PlanQty       DECIMAL(18, 2) NOT NULL,
    Status        INT            NOT NULL CONSTRAINT DF_Mes_WorkOrder_Status DEFAULT (0),
    PlanStart     DATETIME2      NULL,
    PlanEnd       DATETIME2      NULL,
    Remark        NVARCHAR(200)  NULL,
    CreateTime    DATETIME2      NOT NULL CONSTRAINT DF_Mes_WorkOrder_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT         NULL,
    UpdateTime    DATETIME2      NULL,
    UpdateUserId  BIGINT         NULL,
    IsDeleted     BIT            NOT NULL CONSTRAINT DF_Mes_WorkOrder_IsDeleted DEFAULT (0)
);
CREATE UNIQUE INDEX UX_Mes_WorkOrder_OrderNo ON Mes_WorkOrder(OrderNo) WHERE IsDeleted = 0;
