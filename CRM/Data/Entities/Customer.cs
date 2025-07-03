namespace CRM.Data.Entities
{
    /// <summary>
    /// Müşteri entity'si - CRM functionality'si için core entity
    /// Business relationship management ve contact tracking
    /// </summary>
    public class Customer : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(11)]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Vergi numarası 10-11 haneli olmalıdır")]
        public string? TaxNumber { get; set; }

        [StringLength(50)]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^[0-9\s\-\+\(\)]+$", ErrorMessage = "Geçerli telefon numarası giriniz")]
        public string? PhoneNumber { get; set; }

        [StringLength(15)]
        public string? MobileNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? District { get; set; }

        [StringLength(10)]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Posta kodu 5 haneli olmalıdır")]
        public string? PostalCode { get; set; }

        public CustomerType CustomerType { get; set; } = CustomerType.Corporate;

        public bool IsActive { get; set; } = true;

        [StringLength(1000)]
        public string? Notes { get; set; }

        // **CRM SPECIFIC FIELDS**
        public DateTime? LastContactDate { get; set; }
        public decimal? CreditLimit { get; set; }
        public int PaymentTermDays { get; set; } = 30;

        // **İLİŞKİLER**
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();

        // **COMPUTED PROPERTIES**
        public string DisplayName => CompanyName;
        public string FullAddress => $"{Address}, {District}/{City} {PostalCode}".Trim(' ', ',');
    }
}
