using System.ComponentModel.DataAnnotations;

namespace CRM.Data.Entities
{
    /// <summary>
    /// Servis entity'si - Core business logic entity
    /// Service management, tracking ve billing için kritik
    /// </summary>
    public class Service : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public int? AssignedUserId { get; set; }

        [Required]
        public ServiceStatus Status { get; set; } = ServiceStatus.Pending;

        [Required]
        public Priority Priority { get; set; } = Priority.Normal;

        public DateTime? ServiceStartDateTime { get; set; }
        public DateTime? ServiceEndDateTime { get; set; }
        public DateTime? ExpectedCompletionDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tutar negatif olamaz")]
        public decimal? ServiceAmount { get; set; }

        public bool IsWarrantyService { get; set; } = false;

        [StringLength(100)]
        public string? DeviceModel { get; set; }

        [StringLength(100)]
        public string? DeviceSerialNumber { get; set; }

        [StringLength(1000)]
        public string? ProblemDescription { get; set; }

        [StringLength(1000)]
        public string? SolutionDescription { get; set; }

        [StringLength(500)]
        public string? UsedParts { get; set; }

        [StringLength(1000)]
        public string? TechnicianNotes { get; set; }

        [StringLength(1000)]
        public string? CustomerNotes { get; set; }

        // **İLİŞKİLER**
        public virtual Customer Customer { get; set; } = null!;
        public virtual User? AssignedUser { get; set; }
        public virtual ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();

        // **COMPUTED PROPERTIES**
        public string ServiceNumber => $"SRV-{Id:D6}";
        public int DaysSinceCreated => (DateTime.Now - CreatedDate).Days;
        public bool IsOverdue => ExpectedCompletionDate.HasValue && DateTime.Now > ExpectedCompletionDate.Value && Status != ServiceStatus.Completed;
        public double ProgressPercentage
        {
            get
            {
                if (!ServiceTasks.Any()) return 0;
                var completed = ServiceTasks.Count(t => t.IsCompleted);
                return (double)completed / ServiceTasks.Count * 100;
            }
        }
    }
}
