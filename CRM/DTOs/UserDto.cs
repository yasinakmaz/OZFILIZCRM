using System.ComponentModel.DataAnnotations;

namespace CRM.DTOs
{
    /// <summary>
    /// Kullanıcı bilgileri için DTO
    /// CRUD işlemlerinde kullanılır
    /// </summary>
    public class UserDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter arasında olmalıdır")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100, ErrorMessage = "E-posta adresi en fazla 100 karakter olabilir")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad gereklidir")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Ad 2-50 karakter arasında olmalıdır")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad gereklidir")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Soyad 2-50 karakter arasında olmalıdır")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [StringLength(15, ErrorMessage = "Telefon numarası en fazla 15 karakter olabilir")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Kullanıcı rolü seçilmelidir")]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginDate { get; set; }

        [StringLength(500, ErrorMessage = "Profil resmi çok büyük")]
        public string? ProfileImageBase64 { get; set; }

        [StringLength(255, ErrorMessage = "Notlar en fazla 255 karakter olabilir")]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // **COMPUTED PROPERTIES**
        public string FullName => $"{FirstName} {LastName}";
        public string DisplayName => $"{FullName} ({Username})";
        public string RoleDisplayName => GetRoleDisplayName(Role);
        public bool IsOnline => LastLoginDate.HasValue && LastLoginDate.Value > DateTime.Now.AddMinutes(-30);

        private static string GetRoleDisplayName(UserRole role)
        {
            return role switch
            {
                UserRole.SuperAdmin => "Süper Admin",
                UserRole.Admin => "Admin",
                UserRole.Technician => "Teknisyen",
                UserRole.CustomerRepresentative => "Müşteri Temsilcisi",
                UserRole.User => "Kullanıcı",
                _ => role.ToString()
            };
        }
    }
}
