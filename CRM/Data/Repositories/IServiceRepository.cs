namespace CRM.Data.Repositories
{
    /// <summary>
    /// Service entity'si için özelleştirilmiş repository interface
    /// Servis yönetimi ve iş akışı için özel metodlar içerir
    /// </summary>
    public interface IServiceRepository : IRepository<Service>
    {
        Task<IEnumerable<Service>> GetByStatusAsync(ServiceStatus status);
        Task<IEnumerable<Service>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Service>> GetByUserIdAsync(int userId);
        Task<Service?> GetServiceWithDetailsAsync(int serviceId);
        Task<IEnumerable<Service>> GetPendingServicesAsync();
        Task<IEnumerable<Service>> GetActiveServicesAsync();
        Task<IEnumerable<Service>> GetServicesAwaitingApprovalAsync();
        Task<IEnumerable<Service>> GetCompletedServicesAsync();
        Task<(IEnumerable<Service> services, int totalCount)> GetServicesPagedAsync(
            int pageNumber, int pageSize, ServiceStatus? status = null,
            int? customerId = null, string? searchTerm = null);
        Task<IEnumerable<Service>> GetServicesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<ServiceStatus, int>> GetServiceStatusCountsAsync();
        Task<IEnumerable<Service>> GetOverdueServicesAsync();
        Task<decimal> GetTotalServiceAmountByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Service>> GetUserServicesAsync(int userId, ServiceStatus? status = null);
    }
}
