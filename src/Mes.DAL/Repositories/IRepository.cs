using System.Linq.Expressions;
using Mes.Common;
using Mes.Models;
using SqlSugar;

namespace Mes.DAL;

public interface IRepository<T> where T : BaseEntity, new()
{
    ISqlSugarClient Db { get; }

    Task<T?> GetByIdAsync(long id);

    Task<List<T>> GetListAsync(Expression<Func<T, bool>>? where = null);

    Task<PageResult<T>> GetPageAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? where = null);

    Task<int> InsertAsync(T entity);

    Task<int> UpdateAsync(T entity);

    Task<int> SoftDeleteAsync(long id, long? userId = null);
}
