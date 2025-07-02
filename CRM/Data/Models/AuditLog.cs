using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Data.Models
{
    /// <summary>
    /// Sistem genelindeki tüm önemli işlemlerin log kayıtlarını tutan model
    /// Güvenlik, audit trail ve hata takibi için kritik öneme sahiptir
    /// </summary>
    [Table("AuditLogs")]
    public class AuditLog
    {
        /// <summary>
        /// Benzersiz log kimliği - Primary Key
        /// Otomatik artan, sıralı ID yapısı log sıralamasında önemli
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// İşlemi gerçekleştiren kullanıcı ID'si
        /// Hangi kullanıcının hangi işlemi yaptığını takip eder
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Gerçekleştirilen işlem türü
        /// Örn: "CREATE_SERVICE", "UPDATE_CUSTOMER", "LOGIN", "APPROVE_SERVICE"
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// İşlemin hedefi olan entity türü
        /// Örn: "Service", "Customer", "User", "ServiceTask"
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// İşlem uygulanan kaydın ID'si
        /// Hangi kayıt üzerinde işlem yapıldığını gösterir
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// İşlem öncesi veri durumu (JSON format)
        /// Değişiklik takibi için eski değerleri saklar
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; set; }

        /// <summary>
        /// İşlem sonrası veri durumu (JSON format)
        /// Değişiklik takibi için yeni değerleri saklar
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; set; }

        /// <summary>
        /// İşlem hakkında ek açıklama
        /// Özel durumlar ve detaylar için kullanılır
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? Description { get; set; }

        /// <summary>
        /// İşlemin gerçekleştirildiği IP adresi
        /// Güvenlik ve erişim takibi için önemli
        /// </summary>
        [StringLength(45)]
        [Column(TypeName = "nvarchar(45)")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// İşlemin gerçekleştirildiği tarih ve saat
        /// Otomatik olarak set edilir, değiştirilemez
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Navigation Properties

        /// <summary>
        /// İşlemi yapan kullanıcı bilgileri
        /// Log kayıtlarında kullanıcı detaylarına erişim sağlar
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
