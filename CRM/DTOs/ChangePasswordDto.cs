using System.ComponentModel.DataAnnotations;

namespace CRM.DTOs
{
    /// <summary>
    /// Şifre değiştirme işlemi için DTO
    /// Mevcut kullanıcının şifre güncelleme bilgilerini taşır
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mevcut şifre gereklidir")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre 6-100 karakter arasında olmalıdır")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre tekrarı gereklidir")]
        [Compare(nameof(NewPassword), ErrorMessage = "Yeni şifreler eşleşmiyor")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
