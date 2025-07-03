using Microsoft.Extensions.Logging;

namespace CRM.Data.Repositories
{
    /// <summary>
    /// SystemLog repository implementation - audit ve logging specific operations
    /// </summary>
    public class SystemLogRepository : Repository<SystemLog>, ISystemLogRepository
    {
        public SystemLogRepository(TeknikServisDbContext context, ILogger<SystemLogRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<SystemLog>> GetLogsByUserAsync(int userId)
        {
            try
            {
                return await _dbSet
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<SystemLog>> GetLogsByActionAsync(string action)
        {
            try
            {
                return await _dbSet
                    .Where(l => l.Action == action)
                    .OrderByDescending(l => l.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by action: {Action}", action);
                throw;
            }
        }

        public async Task<IEnumerable<SystemLog>> GetLogsByEntityAsync(string entityType, int entityId)
        {
            try
            {
                return await _dbSet
                    .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                    .OrderByDescending(l => l.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by entity: {EntityType} {EntityId}", entityType, entityId);
                throw;
            }
        }

        public async Task<IEnumerable<SystemLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _dbSet
                    .Where(l => l.CreatedDate >= startDate && l.CreatedDate <= endDate)
                    .OrderByDescending(l => l.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by date range");
                throw;
            }
        }

        public async Task<IEnumerable<SystemLog>> GetErrorLogsAsync()
        {
            try
            {
                return await _dbSet
                    .Where(l => l.LogLevel == LogLevel.Error || l.LogLevel == LogLevel.Critical)
                    .OrderByDescending(l => l.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting error logs");
                throw;
            }
        }

        public async Task<(IEnumerable<SystemLog> Logs, int TotalCount)> GetLogsPagedAsync(
            int pageNumber, int pageSize,
            string? action = null,
            string? entityType = null,
            int? userId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                IQueryable<SystemLog> query = _dbSet;

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(l => l.Action == action);

                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(l => l.EntityType == entityType);

                if (userId.HasValue)
                    query = query.Where(l => l.UserId == userId.Value);

                if (startDate.HasValue)
                    query = query.Where(l => l.CreatedDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(l => l.CreatedDate <= endDate.Value);

                var totalCount = await query.CountAsync();
                var logs = await query
                    .OrderByDescending(l => l.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (logs, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs paged");
                throw;
            }
        }

        public async Task CleanupOldLogsAsync(DateTime olderThan)
        {
            try
            {
                var oldLogs = await _dbSet
                    .Where(l => l.CreatedDate < olderThan && l.LogLevel != LogLevel.Error && l.LogLevel != LogLevel.Critical)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _logger.LogInformation("Cleaning up {Count} old logs older than {Date}", oldLogs.Count, olderThan);
                    await DeleteRangeAsync(oldLogs);
                    await SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old logs");
                throw;
            }
        }

        public async Task<long> GetLogSizeAsync()
        {
            try
            {
                return await _dbSet.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log size");
                throw;
            }
        }
    }
}
