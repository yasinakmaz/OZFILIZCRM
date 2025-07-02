using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Data.Models
{
    /// <summary>
    /// Service ve User arasındaki Many-to-Many ilişkiyi temsil eden junction table
    /// Bir servise birden fazla personel atanabilir, bir personel birden fazla serviste çalışabilir
    /// </summary>
    [Table("ServiceUsers")]
    public class ServiceUser
    {
        /// <summary>
        /// Composite Primary Key'in ilk parçası - Servis ID
        /// </summary>
        [Key, Column(Order = 0)]
        public int ServiceId { get; set; }

        /// <summary>
        /// Composite Primary Key'in ikinci parçası - Kullanıcı ID
        /// </summary>
        [Key, Column(Order = 1)]
        public int UserId { get; set; }

        /// <summary>
        /// Personelin bu servise atanma tarihi
        /// Kim ne zaman hangi servise atandı takibi için
        /// </summary>
        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Personeli bu servise atayan yönetici
        /// Atama kararının sorumlusu
        /// </summary>
        [Required]
        public int AssignedByUserId { get; set; }

        /// <summary>
        /// Bu atama aktif mi
        /// Personel servisten çıkarıldığında false yapılır, silinmez
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Atama sonlandırma tarihi
        /// Personel servisten çıkarıldığında set edilir
        /// </summary>
        public DateTime? RemovedDate { get; set; }

        // Navigation Properties

        /// <summary>
        /// İlişkili servis bilgileri
        /// </summary>
        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;

        /// <summary>
        /// İlişkili kullanıcı bilgileri
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Atamayı yapan yönetici bilgileri
        /// </summary>
        [ForeignKey("AssignedByUserId")]
        public virtual User AssignedByUser { get; set; } = null!;
    }
}
