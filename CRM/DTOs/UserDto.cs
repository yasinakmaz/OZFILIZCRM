using System.ComponentModel.DataAnnotations;

namespace CRM.DTOs
{
    /// <summary>
    /// Kullanıcı form işlemleri için veri transfer objesi
    /// Validation attribute'ları ile client-side validation sağlar
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Kullanıcı ID'si - güncellemeler için kullanılır
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Kullanıcı adı - sisteme giriş için kullanılır
        /// </summary>
        [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter arasında olmalıdır")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email adresi - alternatif giriş ve bildirimler için
        /// </summary>
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        [StringLength(200, ErrorMessage = "Email adresi en fazla 200 karakter olabilir")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Şifre - sadece yeni kullanıcı oluştururken zorunlu
        /// </summary>
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre 6-100 karakter arasında olmalıdır")]
        public string? Password { get; set; }

        /// <summary>
        /// Şifre tekrarı - UI'da doğrulama için
        /// </summary>
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
        public string? PasswordConfirm { get; set; }

        /// <summary>
        /// Kullanıcı rolü
        /// </summary>
        [Required(ErrorMessage = "Kullanıcı rolü seçilmelidir")]
        public UserRole Role { get; set; }

        /// <summary>
        /// Profil resmi - Base64 string formatında
        /// </summary>
        public string? ProfileImage { get; set; }

        /// <summary>
        /// Kullanıcının aktif olup olmadığı
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Hesap oluşturulma tarihi - readonly
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Son güncelleme tarihi - readonly
        /// </summary>
        public DateTime? UpdatedDate { get; set; }
    }
}
