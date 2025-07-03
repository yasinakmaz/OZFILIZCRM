using System.ComponentModel.DataAnnotations;

namespace CRM.Data.Entities
{
    /// <summary>
    /// Servis görevi entity'si - Task management ve progress tracking
    /// Granular service tracking ve workflow management için
    /// </summary>
    public class ServiceTask : BaseEntity
    {
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public Priority Priority { get; set; } = Priority.Normal;

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedDate { get; set; }

        public int? CompletedByUserId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public int EstimatedMinutes { get; set; } = 60;

        public int? ActualMinutes { get; set; }

        // **İLİŞKİLER**
        public virtual Service Service { get; set; } = null!;
        public virtual User? CompletedByUser { get; set; }

        // **COMPUTED PROPERTIES**
        public string TaskNumber => $"TSK-{ServiceId:D6}-{Id:D3}";
        public bool IsOverdue => !IsCompleted && Service?.ExpectedCompletionDate.HasValue == true && DateTime.Now > Service.ExpectedCompletionDate.Value;
    }
}
