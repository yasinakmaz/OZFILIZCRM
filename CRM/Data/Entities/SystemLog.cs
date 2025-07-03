using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace CRM.Data.Entities
{
    /// <summary>
    /// Sistem log entity'si - Audit trail ve system monitoring
    /// Security, compliance ve debugging için kritik
    /// </summary>
    public class SystemLog : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public int? UserId { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        [StringLength(4000)]
        public string? ExceptionDetails { get; set; }

        [StringLength(4000)]
        public string? AdditionalData { get; set; }

        // **İLİŞKİLER**
        public virtual User? User { get; set; }

        // **COMPUTED PROPERTIES**
        public string LogEntry => $"[{CreatedDate:yyyy-MM-dd HH:mm:ss}] {Action} - {Description}";
    }
}
