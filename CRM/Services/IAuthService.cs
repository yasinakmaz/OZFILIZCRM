namespace CRM.Services
{
    /// <summary>
    /// Authentication servis interface'i
    /// Login, logout, token management ve session handling
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginDto loginDto);
        Task<AuthResult> LogoutAsync();
        Task<AuthResult> RegisterUserAsync(RegisterDto registerDto, int registeredByUserId);
        Task<AuthResult> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
        Task<AuthResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<bool> ValidateTokenAsync(string token);
        Task<User?> GetCurrentUserAsync();
        Task<SavedCredentialsDto?> GetSavedCredentialsAsync();
        Task SaveCredentialsAsync(LoginDto loginDto, bool rememberMe);
        Task ClearSavedCredentialsAsync();
        bool IsAuthenticated { get; }
        string? CurrentUsername { get; }
        int? CurrentUserId { get; }
        UserRole? CurrentUserRole { get; }
    }
}
