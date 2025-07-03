using Microsoft.Extensions.Logging;
namespace CRM.Data.Repositories
{
    /// <summary>
    /// ServiceTask repository implementation - task management specific operations
    /// </summary>
    public class ServiceTaskRepository : Repository<ServiceTask>, IServiceTaskRepository
    {
        public ServiceTaskRepository(TeknikServisDbContext context, ILogger<ServiceTaskRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<ServiceTask>> GetTasksByServiceAsync(int serviceId)
        {
            try
            {
                return await _dbSet
                    .Include(t => t.CompletedByUser)
                    .Where(t => t.ServiceId == serviceId)
                    .OrderBy(t => t.IsCompleted)
                    .ThenByDescending(t => t.Priority)
                    .ThenBy(t => t.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by service: {ServiceId}", serviceId);
                throw;
            }
        }

        public async Task<IEnumerable<ServiceTask>> GetTasksByUserAsync(int userId)
        {
            try
            {
                return await _dbSet
                    .Include(t => t.Service)
                        .ThenInclude(s => s.Customer)
                    .Where(t => t.CompletedByUserId == userId)
                    .OrderByDescending(t => t.CompletedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ServiceTask>> GetPendingTasksAsync()
        {
            try
            {
                return await _dbSet
                    .Include(t => t.Service)
                        .ThenInclude(s => s.Customer)
                    .Where(t => !t.IsCompleted)
                    .OrderByDescending(t => t.Priority)
                    .ThenBy(t => t.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending tasks");
                throw;
            }
        }

        public async Task<IEnumerable<ServiceTask>> GetCompletedTasksAsync()
        {
            try
            {
                return await _dbSet
                    .Include(t => t.Service)
                        .ThenInclude(s => s.Customer)
                    .Include(t => t.CompletedByUser)
                    .Where(t => t.IsCompleted)
                    .OrderByDescending(t => t.CompletedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed tasks");
                throw;
            }
        }

        public async Task<IEnumerable<ServiceTask>> GetTasksByPriorityAsync(Priority priority)
        {
            try
            {
                return await _dbSet
                    .Include(t => t.Service)
                        .ThenInclude(s => s.Customer)
                    .Where(t => t.Priority == priority && !t.IsCompleted)
                    .OrderBy(t => t.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by priority: {Priority}", priority);
                throw;
            }
        }

        public async Task<bool> CompleteTaskAsync(int taskId, int completedByUserId)
        {
            try
            {
                var task = await GetByIdAsync(taskId);
                if (task != null && !task.IsCompleted)
                {
                    task.IsCompleted = true;
                    task.CompletedDate = DateTime.Now;
                    task.CompletedByUserId = completedByUserId;

                    await UpdateAsync(task);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<int> GetCompletedTaskCountByServiceAsync(int serviceId)
        {
            try
            {
                return await _dbSet.CountAsync(t => t.ServiceId == serviceId && t.IsCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completed task count for service: {ServiceId}", serviceId);
                throw;
            }
        }

        public async Task<int> GetTotalTaskCountByServiceAsync(int serviceId)
        {
            try
            {
                return await _dbSet.CountAsync(t => t.ServiceId == serviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total task count for service: {ServiceId}", serviceId);
                throw;
            }
        }

        public async Task<IEnumerable<ServiceTask>> GetOverdueTasksAsync()
        {
            try
            {
                return await _dbSet
                    .Include(t => t.Service)
                        .ThenInclude(s => s.Customer)
                    .Where(t => !t.IsCompleted &&
                               t.Service.ExpectedCompletionDate.HasValue &&
                               t.Service.ExpectedCompletionDate.Value < DateTime.Now)
                    .OrderBy(t => t.Service.ExpectedCompletionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue tasks");
                throw;
            }
        }

        public async Task<double> GetServiceProgressPercentageAsync(int serviceId)
        {
            try
            {
                var totalTasks = await GetTotalTaskCountByServiceAsync(serviceId);
                if (totalTasks == 0) return 0;

                var completedTasks = await GetCompletedTaskCountByServiceAsync(serviceId);
                return (double)completedTasks / totalTasks * 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service progress percentage: {ServiceId}", serviceId);
                throw;
            }
        }
    }

}
