using System.ComponentModel.DataAnnotations;

namespace CRM.DTOs
{
    /// <summary>
    /// Şifre sıfırlama işlemi için DTO
    /// Admin tarafından kullanıcı şifresi sıfırlama
    /// </summary>
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
        public string Username { get; set; } = string.Empty;

        public string? NewPassword { get; set; }
        public bool GenerateRandomPassword { get; set; } = true;
    }
}
