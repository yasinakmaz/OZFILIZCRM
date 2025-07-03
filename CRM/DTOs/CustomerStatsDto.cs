namespace CRM.DTOs
{
    /// <summary>
    /// Müşteri istatistikleri için DTO
    /// Dashboard ve raporlarda kullanılır
    /// </summary>
    public class CustomerStatsDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int CorporateCustomers { get; set; }
        public int IndividualCustomers { get; set; }
        public int DealerCustomers { get; set; }
        public int CustomersWithActiveServices { get; set; }
        public decimal TotalServiceRevenue { get; set; }
        public decimal AverageServiceValue { get; set; }
    }
}
