namespace CRM.DTOs
{
    /// <summary>
    /// Servis listesi için lightweight DTO
    /// Grid view ve listeleme işlemlerinde kullanılır
    /// </summary>
    public class ServiceListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? AssignedUserName { get; set; }
        public ServiceStatus Status { get; set; }
        public Priority Priority { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }
        public decimal? ServiceAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsOverdue { get; set; }
        public double ProgressPercentage { get; set; }
        public string ServiceNumber => $"SRV-{Id:D6}";
    }
}
