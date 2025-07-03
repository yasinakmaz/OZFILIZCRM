namespace CRM.DTOs
{
    /// <summary>
    /// Dashboard ana ekranı için DTO
    /// Tüm widget'ların verisini içerir
    /// </summary>
    public class DashboardDto
    {
        public ServiceStatsDto ServiceStats { get; set; } = new();
        public CustomerStatsDto CustomerStats { get; set; } = new();
        public UserStatsDto UserStats { get; set; } = new();
        public List<ServiceListDto> RecentServices { get; set; } = new();
        public List<ServiceListDto> OverdueServices { get; set; } = new();
        public List<ServiceListDto> TodayServices { get; set; } = new();
        public List<CustomerSearchDto> TopCustomers { get; set; } = new();
        public List<UserDto> ActiveTechnicians { get; set; } = new();
        public ChartDataDto RevenueChart { get; set; } = new();
        public ChartDataDto ServiceStatusChart { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
