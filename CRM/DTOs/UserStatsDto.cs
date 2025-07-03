namespace CRM.DTOs
{
    /// <summary>
    /// Kullanıcı istatistikleri için DTO
    /// Dashboard ve raporlarda kullanılır
    /// </summary>
    public class UserStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int AdminUsers { get; set; }
        public int TechnicianUsers { get; set; }
        public int CustomerRepUsers { get; set; }
        public DateTime? LastUserRegistration { get; set; }
        public string? MostActiveUser { get; set; }
    }
}
