using SqlSugar;

namespace Mes.Models;

/// <summary>业务异常。Action 为当时的操作名，Detail 为异常全文。</summary>
[SugarTable("Sys_ErrorLog")]
public class SysErrorLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 128)]
    public string Action { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "nvarchar(max)")]
    public string Detail { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public long? CreateUserId { get; set; }
}
