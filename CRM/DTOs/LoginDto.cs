using System.ComponentModel.DataAnnotations;

namespace CRM.DTOs
{
    /// <summary>
    /// Giriş formu için veri transfer objesi
    /// Validation attribute'ları ile form doğrulaması sağlar
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// Kullanıcının email adresi
        /// Hem giriş hem de validation için kullanılır
        /// </summary>
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının şifresi
        /// Plain text olarak alınır, AuthService'de hash ile karşılaştırılır
        /// </summary>
        [Required(ErrorMessage = "Şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Beni hatırla seçeneği
        /// True ise giriş bilgileri SecureStorage'a kaydedilir
        /// </summary>
        public bool RememberMe { get; set; } = false;

        /// <summary>
        /// İsteğin geldiği IP adresi
        /// Güvenlik logları için kullanılır
        /// </summary>
        public string? IpAddress { get; set; }
    }
}
