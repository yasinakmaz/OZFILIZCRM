using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Data.Models
{
    /// <summary>
    /// Servis içindeki yapılacaklar listesini temsil eden model
    /// Her task bir servise aittir ve kendi öncelik seviyesi vardır
    /// </summary>
    [Table("ServiceTasks")]
    public class ServiceTask
    {
        /// <summary>
        /// Benzersiz task kimliği - Primary Key
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Hangi servise ait olduğu - Foreign Key
        /// </summary>
        [Required]
        public int ServiceId { get; set; }

        /// <summary>
        /// Yapılacak işin açıklaması
        /// Detaylı olarak ne yapılacağını belirtir
        /// </summary>
        [Required]
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Bu task'ın aciliyet seviyesi
        /// İş önceliklerini belirlemek için kullanılır
        /// </summary>
        [Required]
        public Priority Priority { get; set; }

        /// <summary>
        /// Task'ın tamamlanıp tamamlanmadığı
        /// Servis ilerlemesini takip etmek için kullanılır
        /// </summary>
        [Required]
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Task'ın tamamlanma tarihi
        /// IsCompleted true olduğunda otomatik set edilir
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Task'ı tamamlayan kullanıcı
        /// Hangi personelin hangi işi yaptığını takip eder
        /// </summary>
        public int? CompletedByUserId { get; set; }

        /// <summary>
        /// Task oluşturulma tarihi
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties

        /// <summary>
        /// İlişkili servis bilgileri
        /// </summary>
        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;

        /// <summary>
        /// Task'ı tamamlayan kullanıcı bilgileri
        /// </summary>
        [ForeignKey("CompletedByUserId")]
        public virtual User? CompletedByUser { get; set; }
    }
}
