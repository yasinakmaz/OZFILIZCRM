using Microsoft.Extensions.Logging;

namespace CRM.Data.Repositories
{
    /// <summary>
    /// Service repository implementation - service management specific operations
    /// </summary>
    public class ServiceRepository : Repository<Service>, IServiceRepository
    {
        public ServiceRepository(TeknikServisDbContext context, ILogger<ServiceRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Service>> GetServicesByStatusAsync(ServiceStatus status)
        {
            try
            {
                return await _dbSet
                    .Include(s => s.Customer)
                    .Include(s => s.AssignedUser)
                    .Where(s => s.Status == status)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by status: {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Service>> GetServicesByCustomerAsync(int customerId)
        {
            try
            {
                return await _dbSet
                    .Include(s => s.AssignedUser)
                    .Include(s => s.ServiceTasks)
                    .Where(s => s.CustomerId == customerId)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by customer: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<IEnumerable<Service>> GetServicesByUserAsync(int userId)
        {
            try
            {
                return await _dbSet
                    .Include(s => s.Customer)
                    .Include(s => s.ServiceTasks)
                    .Where(s => s.AssignedUserId == userId)
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Service>> GetPendingServicesAsync()
        {
            return await GetServicesByStatusAsync(ServiceStatus.Pending);
        }

        public async Task<IEnumerable<Service>> GetServicesInProgressAsync()
        {
            return await GetServicesByStatusAsync(ServiceStatus.InProgress);
        }

        public async Task<IEnumerable<Service>> GetCompletedServicesAsync()
        {
            return await GetServicesByStatusAsync(ServiceStatus.Completed);
        }

        public async Task<IEnumerable<Service>> GetOverdueServicesAsync()
        {
            try
            {
                return await _dbSet
                    .Include(s => s.Customer)
                    .Include(s => s.AssignedUser)
                    .Where(s => s.ExpectedCompletionDate.HasValue &&
                               s.ExpectedCompletionDate.Value < DateTime.Now &&
                               s.Status != ServiceStatus.Completed &&
                               s.Status != ServiceStatus.Cancelled)
                    .OrderBy(s => s.ExpectedCompletionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue services");
                throw;
            }
        }

        public async Task<(IEnumerable<Service> Services, int TotalCount)> GetServicesPagedAsync(
            int pageNumber, int pageSize,
            ServiceStatus? status = null,
            int? customerId = null,
            int? assignedUserId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                IQueryable<Service> query = _dbSet
                    .Include(s => s.Customer)
                    .Include(s => s.AssignedUser);

                if (status.HasValue)
                    query = query.Where(s => s.Status == status.Value);

                if (customerId.HasValue)
                    query = query.Where(s => s.CustomerId == customerId.Value);

                if (assignedUserId.HasValue)
                    query = query.Where(s => s.AssignedUserId == assignedUserId.Value);

                if (startDate.HasValue)
                    query = query.Where(s => s.CreatedDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.CreatedDate <= endDate.Value);

                var totalCount = await query.CountAsync();
                var services = await query
                    .OrderByDescending(s => s.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (services, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services paged");
                throw;
            }
        }

        public async Task<Service?> GetServiceWithDetailsAsync(int serviceId)
        {
            try
            {
                return await _dbSet
                    .Include(s => s.Customer)
                    .Include(s => s.AssignedUser)
                    .Include(s => s.ServiceTasks)
                        .ThenInclude(t => t.CompletedByUser)
                    .FirstOrDefaultAsync(s => s.Id == serviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service with details: {ServiceId}", serviceId);
                throw;
            }
        }

        public async Task<decimal> GetTotalServiceAmountByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _dbSet
                    .Where(s => s.CreatedDate >= startDate &&
                               s.CreatedDate <= endDate &&
                               s.ServiceAmount.HasValue)
                    .SumAsync(s => s.ServiceAmount!.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total service amount by period");
                throw;
            }
        }

        public async Task<IEnumerable<Service>> GetServicesDueTodayAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                return await _dbSet
                    .Include(s => s.Customer)
                    .Include(s => s.AssignedUser)
                    .Where(s => s.ExpectedCompletionDate >= today &&
                               s.ExpectedCompletionDate < tomorrow &&
                               s.Status != ServiceStatus.Completed)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services due today");
                throw;
            }
        }

        public async Task<int> GetActiveServiceCountAsync()
        {
            try
            {
                return await _dbSet.CountAsync(s => s.Status == ServiceStatus.Pending ||
                                                   s.Status == ServiceStatus.InProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active service count");
                throw;
            }
        }
    }
}
