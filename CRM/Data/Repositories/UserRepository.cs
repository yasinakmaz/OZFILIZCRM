using Microsoft.Extensions.Logging;

namespace CRM.Data.Repositories
{
    /// <summary>
    /// User repository implementation - authentication ve user management specific operations
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(TeknikServisDbContext context, ILogger<UserRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            try
            {
                return !await _dbSet.AnyAsync(u => u.Username == username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability: {Username}", username);
                throw;
            }
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            try
            {
                return !await _dbSet.AnyAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability: {Email}", email);
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
        {
            try
            {
                return await _dbSet.Where(u => u.Role == role && u.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {Role}", role);
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            try
            {
                return await _dbSet.Where(u => u.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users");
                throw;
            }
        }

        public async Task<User?> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                var user = await GetByUsernameAsync(username);
                if (user != null && user.IsActive && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return user;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for username: {Username}", username);
                throw;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user != null)
                {
                    user.LastLoginDate = DateTime.Now;
                    await UpdateAsync(user);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                var normalizedSearch = searchTerm.ToLower();
                return await _dbSet
                    .Where(u => u.IsActive &&
                               (u.Username.ToLower().Contains(normalizedSearch) ||
                                u.FirstName.ToLower().Contains(normalizedSearch) ||
                                u.LastName.ToLower().Contains(normalizedSearch) ||
                                u.Email.ToLower().Contains(normalizedSearch)))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPasswordHash)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user != null)
                {
                    user.PasswordHash = newPasswordHash;
                    await UpdateAsync(user);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
                throw;
            }
        }
    }
}
