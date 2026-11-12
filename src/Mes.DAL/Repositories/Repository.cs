using System.Linq.Expressions;
using Mes.Common;
using Mes.Models;
using SqlSugar;

namespace Mes.DAL;

public class Repository<T> : IRepository<T> where T : BaseEntity, new()
{
    public ISqlSugarClient Db => SqlSugarSetup.Db;

    public async Task<T?> GetByIdAsync(long id)
    {
        var rows = await Db.Queryable<T>().Where(it => it.Id == id).Take(1).ToListAsync();
        return rows.FirstOrDefault();
    }

    public Task<List<T>> GetListAsync(Expression<Func<T, bool>>? where = null)
    {
        var query = Db.Queryable<T>();
        if (where != null)
            query = query.Where(where);
        return query.ToListAsync();
    }

    public async Task<PageResult<T>> GetPageAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? where = null)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 20;

        RefAsync<int> total = 0;
        var query = Db.Queryable<T>();
        if (where != null)
            query = query.Where(where);

        var items = await query.ToPageListAsync(pageIndex, pageSize, total);
        return new PageResult<T>(items, total);
    }

    public Task<int> InsertAsync(T entity)
    {
        if (entity.CreateTime == default)
            entity.CreateTime = DateTime.Now;
        return Db.Insertable(entity).ExecuteCommandAsync();
    }

    public Task<int> UpdateAsync(T entity)
    {
        entity.UpdateTime = DateTime.Now;
        return Db.Updateable(entity).ExecuteCommandAsync();
    }

    public Task<int> SoftDeleteAsync(long id, long? userId = null)
    {
        var now = DateTime.Now;
        return Db.Updateable<T>()
            .SetColumns(it => new T
            {
                IsDeleted = true,
                UpdateTime = now,
                UpdateUserId = userId
            })
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }
}
