namespace CRM.Data.Repositories
{
    /// <summary>
    /// User entity için özel repository interface
    /// Authentication ve user management specific operations
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email);
        Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<User?> ValidateCredentialsAsync(string username, string password);
        Task<bool> UpdateLastLoginAsync(int userId);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task<bool> ResetPasswordAsync(int userId, string newPasswordHash);
    }
}
