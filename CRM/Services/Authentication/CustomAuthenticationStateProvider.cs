using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CRM.Services.Authentication
{
    /// <summary>
    /// Custom Authentication State Provider implementation
    /// Blazor ile MAUI authentication entegrasyonu
    /// </summary>
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider, IAuthenticationStateProvider
    {
        private readonly IAuthService _authService;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(IAuthService authService, ILogger<CustomAuthenticationStateProvider> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Mevcut authentication state'ini döndürür
        /// </summary>
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // **CHECK EXISTING AUTH STATE**
                if (_authService.IsAuthenticated)
                {
                    var currentUser = await _authService.GetCurrentUserAsync();
                    if (currentUser != null)
                    {
                        _currentUser = CreateClaimsPrincipal(currentUser);
                        _logger.LogDebug("User authenticated: {Username}", currentUser.Username);
                    }
                    else
                    {
                        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                        _logger.LogDebug("No authenticated user found");
                    }
                }
                else
                {
                    _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                    _logger.LogDebug("User not authenticated");
                }

                return new AuthenticationState(_currentUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authentication state");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        /// <summary>
        /// Kullanıcı giriş yaptığında çağrılır
        /// </summary>
        public async Task NotifyUserAuthenticationAsync(string token, User user)
        {
            try
            {
                _currentUser = CreateClaimsPrincipal(user);

                var authState = new AuthenticationState(_currentUser);
                NotifyAuthenticationStateChanged(Task.FromResult(authState));

                _logger.LogInformation("User authentication notified: {Username}", user.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user authentication");
            }
        }

        /// <summary>
        /// Kullanıcı çıkış yaptığında çağrılır
        /// </summary>
        public async Task NotifyUserLogoutAsync()
        {
            try
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

                var authState = new AuthenticationState(_currentUser);
                NotifyAuthenticationStateChanged(Task.FromResult(authState));

                _logger.LogInformation("User logout notified");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user logout");
            }
        }

        /// <summary>
        /// Authentication state'ini yeniler
        /// </summary>
        public async Task RefreshAuthenticationStateAsync()
        {
            try
            {
                var authState = await GetAuthenticationStateAsync();
                NotifyAuthenticationStateChanged(Task.FromResult(authState));

                _logger.LogDebug("Authentication state refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing authentication state");
            }
        }

        /// <summary>
        /// User entity'sinden ClaimsPrincipal oluşturur
        /// </summary>
        private static ClaimsPrincipal CreateClaimsPrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("FullName", user.FullName),
                new("IsActive", user.IsActive.ToString()),
                new("CreatedDate", user.CreatedDate.ToString("O"))
            };

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
            }

            if (user.LastLoginDate.HasValue)
            {
                claims.Add(new Claim("LastLoginDate", user.LastLoginDate.Value.ToString("O")));
            }

            // **ROLE-BASED PERMISSIONS**
            var permissions = GetRolePermissions(user.Role);
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("Permission", permission));
            }

            var identity = new ClaimsIdentity(claims, "TeknikServisAuth");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Role'e göre permission'ları döndürür
        /// </summary>
        private static List<string> GetRolePermissions(UserRole role)
        {
            return role switch
            {
                UserRole.SuperAdmin => new List<string>
                {
                    "CanViewAllServices", "CanEditAllServices", "CanDeleteServices",
                    "CanManageUsers", "CanViewReports", "CanManageSystem",
                    "CanViewAllCustomers", "CanEditAllCustomers", "CanDeleteCustomers"
                },
                UserRole.Admin => new List<string>
                {
                    "CanViewAllServices", "CanEditAllServices", "CanDeleteServices",
                    "CanManageUsers", "CanViewReports",
                    "CanViewAllCustomers", "CanEditAllCustomers"
                },
                UserRole.Technician => new List<string>
                {
                    "CanViewAssignedServices", "CanEditAssignedServices",
                    "CanViewAllCustomers", "CanEditServiceTasks"
                },
                UserRole.CustomerRepresentative => new List<string>
                {
                    "CanViewAllServices", "CanCreateServices",
                    "CanViewAllCustomers", "CanEditAllCustomers", "CanCreateCustomers"
                },
                UserRole.User => new List<string>
                {
                    "CanViewAssignedServices"
                },
                _ => new List<string>()
            };
        }
    }
}
