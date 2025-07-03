namespace CRM.Services.Dashboard
{
    /// <summary>
    /// Dashboard servisi interface'i
    /// Ana ekran widget'ları ve istatistikleri için
    /// </summary>
    public interface IDashboardService
    {
        Task<ServiceResult<DashboardDto>> GetDashboardDataAsync(int userId);
        Task<ServiceResult<ServiceStatsDto>> GetServiceStatsAsync();
        Task<ServiceResult<CustomerStatsDto>> GetCustomerStatsAsync();
        Task<ServiceResult<UserStatsDto>> GetUserStatsAsync();
        Task<ServiceResult<IEnumerable<ServiceListDto>>> GetRecentServicesAsync(int count = 10);
        Task<ServiceResult<IEnumerable<ServiceListDto>>> GetUserDashboardServicesAsync(int userId, int count = 5);
        Task<ServiceResult<ChartDataDto>> GetRevenueChartDataAsync(int months = 6);
        Task<ServiceResult<ChartDataDto>> GetServiceStatusChartDataAsync();
        Task<ServiceResult<ChartDataDto>> GetMonthlyServiceCountChartAsync(int months = 12);
    }
}
