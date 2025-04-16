using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace UserPermissionSystem.Domain.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        // 查询
        Task<TEntity> GetByIdAsync(object id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
        
        // 增加
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        
        // 更新
        Task UpdateAsync(TEntity entity);
        
        // 删除
        Task RemoveAsync(TEntity entity);
        Task RemoveRangeAsync(IEnumerable<TEntity> entities);
        
        // 其他
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
        
        // 获取查询对象
        IQueryable<TEntity> Query();
    }
}