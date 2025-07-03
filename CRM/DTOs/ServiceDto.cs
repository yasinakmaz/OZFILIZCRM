using System.ComponentModel.DataAnnotations;


namespace CRM.DTOs
{
    /// <summary>
    /// Servis bilgileri için DTO
    /// CRUD işlemlerinde kullanılır
    /// </summary>
    public class ServiceDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Servis başlığı gereklidir")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Servis başlığı 5-200 karakter arasında olmalıdır")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Servis açıklaması gereklidir")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Servis açıklaması 10-2000 karakter arasında olmalıdır")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Müşteri seçilmelidir")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir müşteri seçiniz")]
        public int CustomerId { get; set; }

        public int? AssignedUserId { get; set; }

        [Required(ErrorMessage = "Servis durumu seçilmelidir")]
        public ServiceStatus Status { get; set; } = ServiceStatus.Pending;

        [Required(ErrorMessage = "Öncelik seviyesi seçilmelidir")]
        public Priority Priority { get; set; } = Priority.Normal;

        public DateTime? ServiceStartDateTime { get; set; }
        public DateTime? ServiceEndDateTime { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Servis tutarı negatif olamaz")]
        public decimal? ServiceAmount { get; set; }

        public bool IsWarrantyService { get; set; } = false;

        [StringLength(100, ErrorMessage = "Cihaz modeli en fazla 100 karakter olabilir")]
        public string? DeviceModel { get; set; }

        [StringLength(100, ErrorMessage = "Cihaz seri numarası en fazla 100 karakter olabilir")]
        public string? DeviceSerialNumber { get; set; }

        [StringLength(1000, ErrorMessage = "Problem açıklaması en fazla 1000 karakter olabilir")]
        public string? ProblemDescription { get; set; }

        [StringLength(1000, ErrorMessage = "Çözüm açıklaması en fazla 1000 karakter olabilir")]
        public string? SolutionDescription { get; set; }

        [StringLength(500, ErrorMessage = "Kullanılan parçalar en fazla 500 karakter olabilir")]
        public string? UsedParts { get; set; }

        [StringLength(1000, ErrorMessage = "Teknisyen notları en fazla 1000 karakter olabilir")]
        public string? TechnicianNotes { get; set; }

        [StringLength(1000, ErrorMessage = "Müşteri notları en fazla 1000 karakter olabilir")]
        public string? CustomerNotes { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // **NAVIGATION PROPERTIES**
        public CustomerDto? Customer { get; set; }
        public UserDto? AssignedUser { get; set; }
        public List<ServiceTaskDto> Tasks { get; set; } = new();

        // **COMPUTED PROPERTIES**
        public string ServiceNumber => $"SRV-{Id:D6}";
        public int DaysSinceCreated => (DateTime.Now - CreatedDate).Days;
        public bool IsOverdue => ExpectedCompletionDate.HasValue && DateTime.Now > ExpectedCompletionDate.Value && Status != ServiceStatus.Completed;
        public double ProgressPercentage => Tasks.Any() ? (double)Tasks.Count(t => t.IsCompleted) / Tasks.Count * 100 : 0;
        public string StatusDisplayName => GetStatusDisplayName(Status);
        public string PriorityDisplayName => GetPriorityDisplayName(Priority);

        private static string GetStatusDisplayName(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Pending => "Beklemede",
                ServiceStatus.Accepted => "Kabul Edildi",
                ServiceStatus.InProgress => "Devam Ediyor",
                ServiceStatus.WaitingForParts => "Parça Bekleniyor",
                ServiceStatus.Testing => "Test Ediliyor",
                ServiceStatus.Completed => "Tamamlandı",
                ServiceStatus.Cancelled => "İptal Edildi",
                ServiceStatus.Rejected => "Reddedildi",
                _ => status.ToString()
            };
        }

        private static string GetPriorityDisplayName(Priority priority)
        {
            return priority switch
            {
                Priority.Low => "Düşük",
                Priority.Normal => "Normal",
                Priority.High => "Yüksek",
                Priority.Critical => "Kritik",
                _ => priority.ToString()
            };
        }
    }
}
