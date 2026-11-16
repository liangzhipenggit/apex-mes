/*
    IndustrialMES 建表脚本（SQL Server）
    含账号权限、工单、过站扫码、强制过站/PLC、标签打印、系统设置、审计与异常日志。
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

/* 过站扫码。Source：扫码框 / 手工 / 全局键盘 / 串口枪 */
CREATE TABLE Mes_ScanRecord (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Barcode       NVARCHAR(128) NOT NULL,
    Source        NVARCHAR(32)  NOT NULL,
    PortName      NVARCHAR(32)  NULL,
    Passed        BIT           NOT NULL,
    Message       NVARCHAR(500) NULL,
    CreateTime    DATETIME2     NOT NULL CONSTRAINT DF_Mes_ScanRecord_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT        NULL,
    UpdateTime    DATETIME2     NULL,
    UpdateUserId  BIGINT        NULL,
    IsDeleted     BIT           NOT NULL CONSTRAINT DF_Mes_ScanRecord_IsDeleted DEFAULT (0)
);
CREATE INDEX IX_Mes_ScanRecord_Barcode ON Mes_ScanRecord(Barcode) WHERE IsDeleted = 0;
CREATE INDEX IX_Mes_ScanRecord_CreateTime ON Mes_ScanRecord(CreateTime);

/* 强制过站、向 PLC 写入参数。这两项都要临时授权后才允许发生。 */
CREATE TABLE Mes_RiskOperation (
    Id                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Operation           NVARCHAR(64)  NOT NULL,
    Barcode             NVARCHAR(128) NULL,
    OperatorName        NVARCHAR(64)  NULL,
    AuthorizerName      NVARCHAR(64)  NOT NULL,
    AuthorizerRealName  NVARCHAR(64)  NULL,
    Success             BIT           NOT NULL,
    Message             NVARCHAR(500) NULL,
    CreateTime          DATETIME2     NOT NULL CONSTRAINT DF_Mes_RiskOperation_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId        BIGINT        NULL,
    UpdateTime          DATETIME2     NULL,
    UpdateUserId        BIGINT        NULL,
    IsDeleted           BIT           NOT NULL CONSTRAINT DF_Mes_RiskOperation_IsDeleted DEFAULT (0)
);
CREATE INDEX IX_Mes_RiskOperation_CreateTime ON Mes_RiskOperation(CreateTime);

/* 标签打印。模板参数名与界面一致：MaterialCode、BatchNo、Barcode。 */
CREATE TABLE Mes_LabelPrint (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MaterialCode  NVARCHAR(64)  NOT NULL,
    BatchNo       NVARCHAR(64)  NULL,
    Barcode       NVARCHAR(128) NOT NULL,
    Copies        INT           NOT NULL CONSTRAINT DF_Mes_LabelPrint_Copies DEFAULT (1),
    Engine        NVARCHAR(32)  NOT NULL,
    IsPreview     BIT           NOT NULL CONSTRAINT DF_Mes_LabelPrint_IsPreview DEFAULT (0),
    TemplatePath  NVARCHAR(260) NULL,
    PrinterName   NVARCHAR(128) NULL,
    Success       BIT           NOT NULL,
    Message       NVARCHAR(500) NULL,
    CreateTime    DATETIME2     NOT NULL CONSTRAINT DF_Mes_LabelPrint_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT        NULL,
    UpdateTime    DATETIME2     NULL,
    UpdateUserId  BIGINT        NULL,
    IsDeleted     BIT           NOT NULL CONSTRAINT DF_Mes_LabelPrint_IsDeleted DEFAULT (0)
);
CREATE INDEX IX_Mes_LabelPrint_Barcode ON Mes_LabelPrint(Barcode) WHERE IsDeleted = 0;
CREATE INDEX IX_Mes_LabelPrint_MaterialCode ON Mes_LabelPrint(MaterialCode) WHERE IsDeleted = 0;

/* 系统设置：业务页字体、标签模板、串口枪。 */
CREATE TABLE Sys_Setting (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SettingKey    NVARCHAR(64)   NOT NULL,
    SettingValue  NVARCHAR(500)  NULL,
    Remark        NVARCHAR(200)  NULL,
    CreateTime    DATETIME2      NOT NULL CONSTRAINT DF_Sys_Setting_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT         NULL,
    UpdateTime    DATETIME2      NULL,
    UpdateUserId  BIGINT         NULL,
    IsDeleted     BIT            NOT NULL CONSTRAINT DF_Sys_Setting_IsDeleted DEFAULT (0)
);
CREATE UNIQUE INDEX UX_Sys_Setting_SettingKey ON Sys_Setting(SettingKey) WHERE IsDeleted = 0;

INSERT INTO Sys_Setting (SettingKey, SettingValue, Remark) VALUES
(N'Ui.FontFamily', N'Microsoft YaHei UI', N'业务页字体'),
(N'Ui.FontSize', N'10.5', N'业务页字号'),
(N'Ui.FontBold', N'0', N'业务页是否加粗，0 否 1 是'),
(N'Label.FastReportTemplate', N'labels\material.frx', N'FastReport 模板'),
(N'Label.BarTenderTemplate', N'labels\material.btw', N'BarTender 模板'),
(N'Label.BarTenderExe', N'', N'bartend.exe 路径，空则走 COM'),
(N'Label.PrinterName', N'', N'标签打印机，空则用默认打印机'),
(N'Scan.PortName', N'COM3', N'过站串口扫码枪');

/* 临时授权审计。不保存口令。 */
CREATE TABLE Sys_AuditLog (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Message       NVARCHAR(1000) NOT NULL,
    CreateTime    DATETIME2      NOT NULL CONSTRAINT DF_Sys_AuditLog_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT         NULL
);
CREATE INDEX IX_Sys_AuditLog_CreateTime ON Sys_AuditLog(CreateTime);

CREATE TABLE Sys_ErrorLog (
    Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Action        NVARCHAR(128) NOT NULL,
    Detail        NVARCHAR(MAX) NOT NULL,
    CreateTime    DATETIME2     NOT NULL CONSTRAINT DF_Sys_ErrorLog_CreateTime DEFAULT (SYSDATETIME()),
    CreateUserId  BIGINT        NULL
);
CREATE INDEX IX_Sys_ErrorLog_CreateTime ON Sys_ErrorLog(CreateTime);
