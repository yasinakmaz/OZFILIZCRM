using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CRM.Data.Repositories
{
    /// <summary>
    /// Generic repository implementation - tüm entity'ler için ortak CRUD operasyonları
    /// Repository pattern'ın concrete implementation'ı, DRY prensibini uygular
    /// </summary>
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly TeknikServisDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly ILogger<Repository<TEntity>> _logger;

        public Repository(TeknikServisDbContext context, ILogger<Repository<TEntity>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbSet = _context.Set<TEntity>();
        }

        // **OKUMA OPERASYONLARI**
        public virtual async Task<TEntity?> GetByIdAsync(int id)
        {
            try
            {
                return await _dbSet.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity by ID: {Id}", id);
                throw;
            }
        }

        public virtual async Task<TEntity?> GetByIdAsync(int id, params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                IQueryable<TEntity> query = _dbSet;

                // Include related entities
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }

                return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity by ID with includes: {Id}", id);
                throw;
            }
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            try
            {
                return await _dbSet.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all entities");
                throw;
            }
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                IQueryable<TEntity> query = _dbSet;

                foreach (var include in includes)
                {
                    query = query.Include(include);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all entities with includes");
                throw;
            }
        }

        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                return await _dbSet.Where(predicate).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding entities with predicate");
                throw;
            }
        }

        public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first entity with predicate");
                throw;
            }
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                return await _dbSet.AnyAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking entity existence");
                throw;
            }
        }

        public virtual async Task<int> CountAsync()
        {
            try
            {
                return await _dbSet.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting entities");
                throw;
            }
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                return await _dbSet.CountAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting entities with predicate");
                throw;
            }
        }

        // **YAZMA OPERASYONLARI**
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            try
            {
                _logger.LogInformation("Adding new {EntityType}", typeof(TEntity).Name);

                var result = await _dbSet.AddAsync(entity);
                return result.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity");
                throw;
            }
        }

        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            try
            {
                var entityList = entities.ToList();
                _logger.LogInformation("Adding {Count} {EntityType} entities", entityList.Count, typeof(TEntity).Name);

                await _dbSet.AddRangeAsync(entityList);
                return entityList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity range");
                throw;
            }
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            try
            {
                _logger.LogInformation("Updating {EntityType}", typeof(TEntity).Name);

                _dbSet.Update(entity);
                return await Task.FromResult(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity");
                throw;
            }
        }

        public virtual async Task DeleteAsync(TEntity entity)
        {
            try
            {
                _logger.LogInformation("Deleting {EntityType}", typeof(TEntity).Name);

                _dbSet.Remove(entity);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity");
                throw;
            }
        }

        public virtual async Task DeleteAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity != null)
                {
                    await DeleteAsync(entity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity by ID: {Id}", id);
                throw;
            }
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities)
        {
            try
            {
                var entityList = entities.ToList();
                _logger.LogInformation("Deleting {Count} {EntityType} entities", entityList.Count, typeof(TEntity).Name);

                _dbSet.RemoveRange(entityList);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity range");
                throw;
            }
        }

        // **SAYFALAMA VE SIRALAMA**
        public virtual async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                IQueryable<TEntity> query = _dbSet;

                // Include related entities
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }

                // Apply filter
                if (filter != null)
                {
                    query = query.Where(filter);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply ordering
                if (orderBy != null)
                {
                    query = orderBy(query);
                }

                // Apply pagination
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged entities");
                throw;
            }
        }

        // **BATCH OPERASYONLAR**
        public virtual async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes");
                throw;
            }
        }
    }
}
