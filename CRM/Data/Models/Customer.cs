using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Data.Models
{
    /// <summary>
    /// Müşteri firmalarını temsil eden model
    /// Hem bireysel hem de kurumsal müşteriler için tasarlanmıştır
    /// </summary>
    [Table("Customers")]
    public class Customer
    {
        /// <summary>
        /// Benzersiz müşteri kimliği - Primary Key
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Firma adı veya bireysel müşteri için ad-soyad
        /// Arama işlemlerinde en çok kullanılan alan
        /// </summary>
        [Required]
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Müşteri tipini belirler - Bireysel mi Kurumsal mı
        /// Fatura ve vergi hesaplamalarında kritik rol oynar
        /// </summary>
        [Required]
        public CompanyType CompanyType { get; set; }

        /// <summary>
        /// Vergi Numarası veya TCKN
        /// CompanyType'a göre farklı validation uygulanır
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string TaxNumber { get; set; } = string.Empty;

        /// <summary>
        /// Vergi Dairesi - sadece kurumsal müşteriler için zorunlu
        /// Bireysel müşteriler için null olabilir
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? TaxOffice { get; set; }

        /// <summary>
        /// Yetkili kişinin adı ve soyadı
        /// İletişim kurarken kiminle konuşulacağını belirtir
        /// </summary>
        [Required]
        [StringLength(150)]
        [Column(TypeName = "nvarchar(150)")]
        public string AuthorizedPersonName { get; set; } = string.Empty;

        /// <summary>
        /// Ana iletişim telefon numarası
        /// Servis sırasında acil ulaşım için kullanılır
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Müşterinin aktif olup olmadığı
        /// Pasif müşteriler için yeni servis oluşturulamaz
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Müşteri kaydının oluşturulma tarihi
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Navigation Property: Bu müşteriye ait tüm servisler
        /// One-to-Many ilişki
        /// </summary>
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
