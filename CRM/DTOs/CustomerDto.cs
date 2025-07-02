using System.ComponentModel.DataAnnotations;


namespace CRM.DTOs
{
    /// <summary>
    /// Müşteri form işlemleri için veri transfer objesi
    /// Validation attribute'ları ile client-side validation sağlar
    /// </summary>
    public class CustomerDto
    {
        /// <summary>
        /// Müşteri ID'si - güncellemeler için kullanılır
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Firma adı veya bireysel müşteri için ad-soyad
        /// </summary>
        [Required(ErrorMessage = "Firma adı gereklidir")]
        [StringLength(200, ErrorMessage = "Firma adı en fazla 200 karakter olabilir")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Müşteri tipi - Bireysel veya Kurumsal
        /// </summary>
        [Required(ErrorMessage = "Firma tipi seçilmelidir")]
        public CompanyType CompanyType { get; set; }

        /// <summary>
        /// Vergi numarası veya TCKN
        /// </summary>
        [Required(ErrorMessage = "Vergi numarası/TCKN gereklidir")]
        [StringLength(50, ErrorMessage = "Vergi numarası en fazla 50 karakter olabilir")]
        public string TaxNumber { get; set; } = string.Empty;

        /// <summary>
        /// Vergi dairesi - sadece kurumsal müşteriler için zorunlu
        /// </summary>
        [StringLength(100, ErrorMessage = "Vergi dairesi en fazla 100 karakter olabilir")]
        public string? TaxOffice { get; set; }

        /// <summary>
        /// Yetkili kişinin adı ve soyadı
        /// </summary>
        [Required(ErrorMessage = "Yetkili kişi adı gereklidir")]
        [StringLength(150, ErrorMessage = "Yetkili kişi adı en fazla 150 karakter olabilir")]
        public string AuthorizedPersonName { get; set; } = string.Empty;

        /// <summary>
        /// İletişim telefon numarası
        /// </summary>
        [Required(ErrorMessage = "Telefon numarası gereklidir")]
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Müşterinin aktif olup olmadığı
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
