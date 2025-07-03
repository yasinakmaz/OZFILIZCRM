namespace CRM.Services.Authentication
{
    /// <summary>
    /// Custom Authentication State Provider interface
    /// Blazor authentication integration için
    /// </summary>
    public interface IAuthenticationStateProvider
    {
        Task<AuthenticationState> GetAuthenticationStateAsync();
        Task NotifyUserAuthenticationAsync(string token, User user);
        Task NotifyUserLogoutAsync();
        Task RefreshAuthenticationStateAsync();
    }
}
