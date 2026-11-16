using Mes.DAL;
using SqlSugar;

namespace Mes.BLL;

/// <summary>
/// 在同一条连接上完成事务。自动关闭连接时，BeginTran 之后的命令会换连接，提交就会报事务已结束。
/// </summary>
internal static class DbWrite
{
    public static async Task RunAsync(Func<ISqlSugarClient, Task> action)
    {
        var db = SqlSugarSetup.Db.CopyNew();
        var config = db.CurrentConnectionConfig;
        var autoClose = config.IsAutoCloseConnection;
        config.IsAutoCloseConnection = false;
        try
        {
            await db.Ado.BeginTranAsync();
            try
            {
                await action(db);
                await db.Ado.CommitTranAsync();
            }
            catch
            {
                try
                {
                    await db.Ado.RollbackTranAsync();
                }
                catch
                {
                    // 事务已经结束时，保留前面的原始异常。
                }

                throw;
            }
        }
        finally
        {
            config.IsAutoCloseConnection = autoClose;
            db.Close();
        }
    }
}
