namespace CRM.DTOs
{
    /// <summary>
    /// Müşteri arama sonuçları için lightweight DTO
    /// Autocomplete ve search işlemlerinde kullanılır
    /// </summary>
    public class CustomerSearchDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public CustomerType CustomerType { get; set; }
        public string DisplayText => $"{CompanyName} - {ContactPerson}";
    }
}
