using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.Data.Models
{
    /// <summary>
    /// Servis kayıtlarını temsil eden ana model
    /// Sistemin iş akışının merkezinde yer alır
    /// </summary>
    [Table("Services")]
    public class Service
    {
        /// <summary>
        /// Benzersiz servis kimliği - Primary Key
        /// Aynı zamanda servis numarası olarak da kullanılır
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Müşteri ile ilişki - Foreign Key
        /// Her servis mutlaka bir müşteriye aittir
        /// </summary>
        [Required]
        public int CustomerId { get; set; }

        /// <summary>
        /// Servisi oluşturan kullanıcı - Foreign Key
        /// Audit trail için kritik, kim hangi servisi açmış takibi
        /// </summary>
        [Required]
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// Servisin mevcut durumu
        /// İş akışının hangi aşamasında olduğunu gösterir
        /// </summary>
        [Required]
        public ServiceStatus Status { get; set; } = ServiceStatus.Pending;

        /// <summary>
        /// Servis tutarı - nullable çünkü başlangıçta belirli olmayabilir
        /// Decimal kullanımı para birimi hesaplamalarında precision sağlar
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ServiceAmount { get; set; }

        /// <summary>
        /// Planlanan servis tarihi ve saati
        /// Müşteri ile anlaşılan randevu zamanı
        /// </summary>
        [Required]
        public DateTime ScheduledDateTime { get; set; }

        /// <summary>
        /// Servise çıkış tarihi ve saati
        /// Personelin fiilen servise çıktığı zaman
        /// </summary>
        public DateTime? ServiceStartDateTime { get; set; }

        /// <summary>
        /// Servis tamamlanma tarihi ve saati
        /// İş bittiğinde otomatik olarak set edilir
        /// </summary>
        public DateTime? ServiceEndDateTime { get; set; }

        /// <summary>
        /// Servis kaydının oluşturulma tarihi
        /// Sistem audit'i için otomatik set edilir
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Son güncelleme tarihi
        /// Her status değişikliğinde güncellenir
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Admin onay tarihi
        /// Sadece admin onayladığında set edilir
        /// </summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>
        /// Onaylayan admin kullanıcısı
        /// Hangi admin'in onayladığını takip eder
        /// </summary>
        public int? ApprovedByUserId { get; set; }

        /// <summary>
        /// Servis notları ve açıklamaları
        /// Özel durumlar ve detaylar için kullanılır
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }

        // Navigation Properties

        /// <summary>
        /// İlişkili müşteri bilgileri
        /// Include ile eager loading yapılabilir
        /// </summary>
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        /// <summary>
        /// Servisi oluşturan kullanıcı bilgileri
        /// </summary>
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; } = null!;

        /// <summary>
        /// Onaylayan admin bilgileri - nullable çünkü henüz onaylanmamış olabilir
        /// </summary>
        [ForeignKey("ApprovedByUserId")]
        public virtual User? ApprovedByUser { get; set; }

        /// <summary>
        /// Bu servise atanmış tüm personeller
        /// Many-to-Many ilişki ServiceUser tablosu üzerinden
        /// </summary>
        public virtual ICollection<ServiceUser> ServiceUsers { get; set; } = new List<ServiceUser>();

        /// <summary>
        /// Bu servise ait yapılacaklar listesi
        /// One-to-Many ilişki
        /// </summary>
        public virtual ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();
    }
}
