using Mes.Models;
using SqlSugar;

namespace Mes.DAL;

public static class SqlSugarSetup
{
    private static readonly object Sync = new();
    private static ISqlSugarClient? _db;

    public static void Init(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("连接字符串不能为空。", nameof(connectionString));

        lock (Sync)
        {
            _db = new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            },
            db =>
            {
                db.QueryFilter.AddTableFilter<BaseEntity>(it => it.IsDeleted == false);
#if DEBUG
                db.Aop.OnLogExecuting = (sql, _) => System.Diagnostics.Debug.WriteLine(sql);
#endif
            });
        }
    }

    public static ISqlSugarClient Db
        => _db ?? throw new InvalidOperationException("数据库未初始化，请先调用 SqlSugarSetup.Init。");

    /// <summary>真正打开一次连接。初始化本身不会连库。</summary>
    public static void EnsureConnected()
    {
        var db = Db;
        db.Ado.Open();
        db.Ado.ExecuteCommand("SELECT 1");
        db.Ado.Close();
    }
}
