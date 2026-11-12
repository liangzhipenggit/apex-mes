using SqlSugar;

namespace Mes.Models;

/// <summary>
/// 业务表公共字段。软删除由仓储统一处理。
/// </summary>
public abstract class BaseEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public long? CreateUserId { get; set; }

    public DateTime? UpdateTime { get; set; }

    public long? UpdateUserId { get; set; }

    public bool IsDeleted { get; set; }
}
