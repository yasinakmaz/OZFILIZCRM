using System.ComponentModel.DataAnnotations;


namespace CRM.DTOs
{
    /// <summary>
    /// Müşteri bilgileri için DTO
    /// CRUD işlemlerinde kullanılır
    /// </summary>
    public class CustomerDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Şirket adı gereklidir")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Şirket adı 2-100 karakter arasında olmalıdır")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(11, MinimumLength = 10, ErrorMessage = "Vergi numarası 10-11 haneli olmalıdır")]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Vergi numarası sadece rakam içermeli")]
        public string? TaxNumber { get; set; }

        [StringLength(50, ErrorMessage = "İletişim kişisi en fazla 50 karakter olabilir")]
        public string? ContactPerson { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta adresi en fazla 100 karakter olabilir")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [StringLength(15, ErrorMessage = "Telefon numarası en fazla 15 karakter olabilir")]
        public string? PhoneNumber { get; set; }

        [Phone(ErrorMessage = "Geçerli bir cep telefonu numarası giriniz")]
        [StringLength(15, ErrorMessage = "Cep telefonu numarası en fazla 15 karakter olabilir")]
        public string? MobileNumber { get; set; }

        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
        public string? Address { get; set; }

        [StringLength(50, ErrorMessage = "Şehir en fazla 50 karakter olabilir")]
        public string? City { get; set; }

        [StringLength(50, ErrorMessage = "İlçe en fazla 50 karakter olabilir")]
        public string? District { get; set; }

        [StringLength(10, ErrorMessage = "Posta kodu en fazla 10 karakter olabilir")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Posta kodu 5 haneli olmalıdır")]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Müşteri tipi seçilmelidir")]
        public CustomerType CustomerType { get; set; } = CustomerType.Corporate;

        public bool IsActive { get; set; } = true;

        [StringLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir")]
        public string? Notes { get; set; }

        public DateTime? LastContactDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Kredi limiti negatif olamaz")]
        public decimal? CreditLimit { get; set; }

        [Range(1, 365, ErrorMessage = "Ödeme vadesi 1-365 gün arasında olmalıdır")]
        public int PaymentTermDays { get; set; } = 30;

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // **COMPUTED PROPERTIES**
        public string DisplayName => CompanyName;
        public string FullAddress => $"{Address}, {District}/{City} {PostalCode}".Trim(' ', ',');
        public int ActiveServiceCount { get; set; }
        public decimal TotalServiceAmount { get; set; }
    }
}
