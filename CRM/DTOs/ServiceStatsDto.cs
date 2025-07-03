namespace CRM.DTOs
{
    /// <summary>
    /// Servis istatistikleri için DTO
    /// Dashboard ve raporlarda kullanılır
    /// </summary>
    public class ServiceStatsDto
    {
        public int TotalServices { get; set; }
        public int PendingServices { get; set; }
        public int InProgressServices { get; set; }
        public int CompletedServices { get; set; }
        public int OverdueServices { get; set; }
        public int TodayServices { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double AverageCompletionDays { get; set; }
        public double CustomerSatisfactionRate { get; set; }
    }
}
