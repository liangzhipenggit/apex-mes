using SqlSugar;

namespace Mes.Models;

/// <summary>临时授权等审计。只记操作说明，不记口令。</summary>
[SugarTable("Sys_AuditLog")]
public class SysAuditLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public long? CreateUserId { get; set; }
}
