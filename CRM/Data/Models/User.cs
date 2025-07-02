using CRM.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Data.Models
{
    /// <summary>
    /// Sistem kullanıcılarını temsil eden ana model
    /// Kimlik doğrulama, yetkilendirme ve kişisel bilgileri içerir
    /// </summary>
    [Table("Users")]
    public class User
    {
        /// <summary>
        /// Benzersiz kullanıcı kimliği - Primary Key
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Kullanıcının sisteme giriş için kullandığı benzersiz isim
        /// Email olabilir veya özel kullanıcı adı olabilir
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının email adresi - bildirimler için kullanılacak
        /// Aynı zamanda alternatif login yöntemi olarak kullanılabilir
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt ile hash'lenmiş şifre
        /// Güvenlik için asla plain text olarak saklanmaz
        /// </summary>
        [Required]
        [StringLength(255)]
        [Column(TypeName = "nvarchar(255)")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının sistemdeki yetki seviyesi
        /// Business logic'te önemli rol oynar
        /// </summary>
        [Required]
        public UserRole Role { get; set; }

        /// <summary>
        /// Kullanıcının profil fotoğrafı
        /// Base64 string olarak veritabanında saklanır
        /// Performans için küçük boyutlu tutulmalıdır
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ProfileImage { get; set; }

        /// <summary>
        /// Kullanıcının aktif olup olmadığı
        /// Pasif kullanıcılar sisteme giremez ama veri bütünlüğü için silinmez
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Hesap oluşturulma tarihi
        /// Audit trail için önemli
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Son güncelleme tarihi
        /// Her profil güncellemesinde otomatik olarak güncellenir
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Navigation Property: Bu kullanıcının atandığı servisler
        /// Many-to-Many ilişki için kullanılır
        /// </summary>
        public virtual ICollection<ServiceUser> ServiceUsers { get; set; } = new List<ServiceUser>();

        /// <summary>
        /// Navigation Property: Bu kullanıcının oluşturduğu servislerin audit logu
        /// Kim hangi servisi oluşturdu takibi için
        /// </summary>
        public virtual ICollection<Service> CreatedServices { get; set; } = new List<Service>();
    }
}
