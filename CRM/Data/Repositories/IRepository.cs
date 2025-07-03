using System.Linq.Expressions;

namespace CRM.Data.Repositories
{
    /// <summary>
    /// Generic repository interface - tüm entity'ler için ortak CRUD operasyonları
    /// Repository pattern'ın temelini oluşturur ve code reusability sağlar
    /// </summary>
    public interface IRepository<TEntity> where TEntity : class
    {
        // **OKUMA OPERASYONLARI**
        Task<TEntity?> GetByIdAsync(int id);
        Task<TEntity?> GetByIdAsync(int id, params Expression<Func<TEntity, object>>[] includes);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<IEnumerable<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes);
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate);

        // **YAZMA OPERASYONLARI**
        Task<TEntity> AddAsync(TEntity entity);
        Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task DeleteAsync(int id);
        Task DeleteRangeAsync(IEnumerable<TEntity> entities);

        // **SAYFALAMA VE SIRALAMA**
        Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] includes);

        // **BATCH OPERASYONLAR**
        Task<int> SaveChangesAsync();
    }
}
