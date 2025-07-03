namespace CRM.DTOs
{
    /// <summary>
    /// Kullanıcı profili için DTO
    /// Profil sayfasında kullanılır
    /// </summary>
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? ProfileImageBase64 { get; set; }
        public DateTime CreatedDate { get; set; }

        // **STATISTICS**
        public int AssignedServicesCount { get; set; }
        public int CompletedServicesCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public double AverageRating { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }
}
