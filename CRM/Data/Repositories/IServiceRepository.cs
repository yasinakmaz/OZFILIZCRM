namespace CRM.Data.Repositories
{
    /// <summary>
    /// Service entity için özel repository interface
    /// Service management ve business logic operations
    /// </summary>
    public interface IServiceRepository : IRepository<Service>
    {
        Task<IEnumerable<Service>> GetServicesByStatusAsync(ServiceStatus status);
        Task<IEnumerable<Service>> GetServicesByCustomerAsync(int customerId);
        Task<IEnumerable<Service>> GetServicesByUserAsync(int userId);
        Task<IEnumerable<Service>> GetPendingServicesAsync();
        Task<IEnumerable<Service>> GetServicesInProgressAsync();
        Task<IEnumerable<Service>> GetCompletedServicesAsync();
        Task<IEnumerable<Service>> GetOverdueServicesAsync();
        Task<(IEnumerable<Service> Services, int TotalCount)> GetServicesPagedAsync(
            int pageNumber, int pageSize,
            ServiceStatus? status = null,
            int? customerId = null,
            int? assignedUserId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
        Task<Service?> GetServiceWithDetailsAsync(int serviceId);
        Task<decimal> GetTotalServiceAmountByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Service>> GetServicesDueTodayAsync();
        Task<int> GetActiveServiceCountAsync();
    }
}
