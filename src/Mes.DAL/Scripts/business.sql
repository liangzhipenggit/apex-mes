/*
    按当前程序实际功能补齐业务表。全新库执行 init.sql 已包含这些表，无需再跑本脚本。
    已有库（只跑过旧版 init.sql）执行本脚本即可，重复执行不会重建已有表。

    对应界面：
      过站防错  Mes_ScanRecord、Mes_RiskOperation
      标签打印  Mes_LabelPrint
      系统设置  Sys_Setting
      临时授权  Sys_AuditLog
      异常日志  Sys_ErrorLog
*/

IF OBJECT_ID(N'dbo.Mes_ScanRecord', N'U') IS NULL
BEGIN
    CREATE TABLE Mes_ScanRecord (
        Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Barcode       NVARCHAR(128) NOT NULL,
        Source        NVARCHAR(32)  NOT NULL, -- 扫码框 / 手工 / 全局键盘 / 串口枪
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
END;

IF OBJECT_ID(N'dbo.Mes_RiskOperation', N'U') IS NULL
BEGIN
    CREATE TABLE Mes_RiskOperation (
        Id                  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Operation           NVARCHAR(64)  NOT NULL, -- 强制过站 / 向PLC写入参数
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
END;

IF OBJECT_ID(N'dbo.Mes_LabelPrint', N'U') IS NULL
BEGIN
    CREATE TABLE Mes_LabelPrint (
        Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MaterialCode  NVARCHAR(64)  NOT NULL,
        BatchNo       NVARCHAR(64)  NULL,
        Barcode       NVARCHAR(128) NOT NULL,
        Copies        INT           NOT NULL CONSTRAINT DF_Mes_LabelPrint_Copies DEFAULT (1),
        Engine        NVARCHAR(32)  NOT NULL, -- FastReport / BarTender
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
END;

IF OBJECT_ID(N'dbo.Sys_Setting', N'U') IS NULL
BEGIN
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
END;

IF NOT EXISTS (SELECT 1 FROM Sys_Setting WHERE SettingKey = N'Ui.FontFamily' AND IsDeleted = 0)
    INSERT INTO Sys_Setting (SettingKey, SettingValue, Remark) VALUES
    (N'Ui.FontFamily', N'Microsoft YaHei UI', N'业务页字体'),
    (N'Ui.FontSize', N'10.5', N'业务页字号'),
    (N'Ui.FontBold', N'0', N'业务页是否加粗，0 否 1 是'),
    (N'Label.FastReportTemplate', N'labels\material.frx', N'FastReport 模板'),
    (N'Label.BarTenderTemplate', N'labels\material.btw', N'BarTender 模板'),
    (N'Label.BarTenderExe', N'', N'bartend.exe 路径，空则走 COM'),
    (N'Label.PrinterName', N'', N'标签打印机，空则用默认打印机'),
    (N'Scan.PortName', N'COM3', N'过站串口扫码枪');

IF OBJECT_ID(N'dbo.Sys_AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE Sys_AuditLog (
        Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Message       NVARCHAR(1000) NOT NULL,
        CreateTime    DATETIME2      NOT NULL CONSTRAINT DF_Sys_AuditLog_CreateTime DEFAULT (SYSDATETIME()),
        CreateUserId  BIGINT         NULL
    );
    CREATE INDEX IX_Sys_AuditLog_CreateTime ON Sys_AuditLog(CreateTime);
END;

IF OBJECT_ID(N'dbo.Sys_ErrorLog', N'U') IS NULL
BEGIN
    CREATE TABLE Sys_ErrorLog (
        Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Action        NVARCHAR(128) NOT NULL,
        Detail        NVARCHAR(MAX) NOT NULL,
        CreateTime    DATETIME2     NOT NULL CONSTRAINT DF_Sys_ErrorLog_CreateTime DEFAULT (SYSDATETIME()),
        CreateUserId  BIGINT        NULL
    );
    CREATE INDEX IX_Sys_ErrorLog_CreateTime ON Sys_ErrorLog(CreateTime);
END;
