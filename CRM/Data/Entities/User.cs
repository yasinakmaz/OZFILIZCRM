using System.ComponentModel.DataAnnotations;


namespace CRM.Data.Entities
{
    /// <summary>
    /// Kullanıcı entity'si - Authentication ve user management için
    /// Security best practices uygulanmış, password hashing dahil
    /// </summary>
    public class User : BaseEntity
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Technician;

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginDate { get; set; }

        [StringLength(500)]
        public string? ProfileImageBase64 { get; set; }

        [StringLength(255)]
        public string? Notes { get; set; }

        // **İLİŞKİLER**
        public virtual ICollection<Service> AssignedServices { get; set; } = new List<Service>();
        public virtual ICollection<ServiceTask> CompletedTasks { get; set; } = new List<ServiceTask>();
        public virtual ICollection<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();

        // **COMPUTED PROPERTIES**
        public string FullName => $"{FirstName} {LastName}";
        public string DisplayName => $"{FullName} ({Username})";
    }
}
