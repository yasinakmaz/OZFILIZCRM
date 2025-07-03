namespace CRM.DTOs
{
    /// <summary>
    /// Kaydedilmiş giriş bilgileri için DTO
    /// "Beni Hatırla" özelliği için kullanılır
    /// </summary>
    public class SavedCredentialsDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
    }
}
