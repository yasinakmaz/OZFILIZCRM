using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CRM.Services
{
    /// <summary>
    /// Kimlik doğrulama ve oturum yönetimi servisi
    /// Login/logout işlemleri, şifre doğrulaması ve session management
    /// </summary>
    public class AuthService
    {
        private readonly TeknikServisDbContext _context;
        private readonly ISecureStorage _secureStorage;
        private readonly LoggingService _loggingService;
        private User? _currentUser;

        // SecureStorage anahtarları - sabit string'lerin const olarak tanımlanması hata riskini azaltır
        private const string SAVED_EMAIL_KEY = "saved_email";
        private const string SAVED_PASSWORD_KEY = "saved_password";
        private const string REMEMBER_ME_KEY = "remember_me";
        private const string CURRENT_USER_KEY = "current_user";

        /// <summary>
        /// Constructor - DI container'dan gerekli servisleri alır
        /// </summary>
        public AuthService(TeknikServisDbContext context, ISecureStorage secureStorage, LoggingService loggingService)
        {
            _context = context;
            _secureStorage = secureStorage;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Mevcut oturum açmış kullanıcıyı döndürür
        /// Lazy loading pattern - ilk erişimde yüklenir, sonrasında cache'den döner
        /// </summary>
        public User? CurrentUser => _currentUser;

        /// <summary>
        /// Kullanıcının oturum açıp açmadığını kontrol eder
        /// </summary>
        public bool IsAuthenticated => _currentUser != null;

        /// <summary>
        /// Email ve şifre ile oturum açma işlemi
        /// BCrypt ile şifre doğrulaması yapar ve başarılı girişleri loglar
        /// </summary>
        /// <param name="loginDto">Giriş bilgileri</param>
        /// <returns>Başarılı ise true, değilse false</returns>
        public async Task<bool> LoginAsync(LoginDto loginDto)
        {
            try
            {
                // Email adresine göre kullanıcıyı bul
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

                if (user == null)
                {
                    await _loggingService.LogAsync("LOGIN_FAILED", "User", null,
                        $"Email not found: {loginDto.Email}", loginDto.IpAddress);
                    return false;
                }

                // BCrypt ile şifre doğrulaması
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    await _loggingService.LogAsync("LOGIN_FAILED", "User", user.Id,
                        $"Invalid password for user: {user.Email}", loginDto.IpAddress, user.Id);
                    return false;
                }

                // Başarılı giriş - kullanıcıyı session'a kaydet
                _currentUser = user;

                // "Beni Hatırla" özelliği aktifse bilgileri güvenli şekilde sakla
                if (loginDto.RememberMe)
                {
                    await SaveCredentialsAsync(loginDto.Email, loginDto.Password);
                }
                else
                {
                    await ClearSavedCredentialsAsync();
                }

                // Mevcut kullanıcı bilgilerini SecureStorage'a kaydet (session için)
                var userJson = JsonSerializer.Serialize(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role,
                    user.ProfileImage
                });
                await _secureStorage.SetAsync(CURRENT_USER_KEY, userJson);

                // Başarılı girişi logla
                await _loggingService.LogAsync("LOGIN_SUCCESS", "User", user.Id,
                    $"User logged in successfully: {user.Email}", loginDto.IpAddress, user.Id);

                return true;
            }
            catch (Exception ex)
            {
                await _loggingService.LogAsync("LOGIN_ERROR", "User", null,
                    $"Login error: {ex.Message}", loginDto.IpAddress);
                return false;
            }
        }

        /// <summary>
        /// Oturum kapatma işlemi
        /// Memory ve SecureStorage'dan kullanıcı bilgilerini temizler
        /// </summary>
        public async Task LogoutAsync()
        {
            if (_currentUser != null)
            {
                await _loggingService.LogAsync("LOGOUT", "User", _currentUser.Id,
                    $"User logged out: {_currentUser.Email}", userId: _currentUser.Id);
            }

            // Memory'den temizle
            _currentUser = null;

            // SecureStorage'dan session bilgilerini temizle
            await _secureStorage.RemoveAsync(CURRENT_USER_KEY);
        }

        /// <summary>
        /// Kaydedilmiş giriş bilgilerini (Beni Hatırla) yükler
        /// Uygulama başlangıcında kullanılır
        /// </summary>
        /// <returns>Kaydedilmiş giriş bilgileri veya null</returns>
        public async Task<LoginDto?> GetSavedCredentialsAsync()
        {
            try
            {
                var rememberMe = await _secureStorage.GetAsync(REMEMBER_ME_KEY);
                if (rememberMe != "true") return null;

                var email = await _secureStorage.GetAsync(SAVED_EMAIL_KEY);
                var password = await _secureStorage.GetAsync(SAVED_PASSWORD_KEY);

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    return null;

                return new LoginDto
                {
                    Email = email,
                    Password = password,
                    RememberMe = true
                };
            }
            catch
            {
                // SecureStorage hatası durumunda null döner
                return null;
            }
        }

        /// <summary>
        /// Önceki oturumdan kalan kullanıcı bilgilerini yükler
        /// Uygulama başlangıcında otomatik giriş için kullanılır
        /// </summary>
        public async Task<bool> RestoreSessionAsync()
        {
            try
            {
                var userJson = await _secureStorage.GetAsync(CURRENT_USER_KEY);
                if (string.IsNullOrEmpty(userJson)) return false;

                var userData = JsonSerializer.Deserialize<dynamic>(userJson);
                if (userData == null) return false;

                // Veritabanından güncel kullanıcı bilgilerini al
                var userId = Convert.ToInt32(userData.GetProperty("Id").GetInt32());
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    await ClearSessionAsync();
                    return false;
                }

                _currentUser = user;

                await _loggingService.LogAsync("SESSION_RESTORED", "User", user.Id,
                    $"Session restored for user: {user.Email}", userId: user.Id);

                return true;
            }
            catch
            {
                await ClearSessionAsync();
                return false;
            }
        }

        /// <summary>
        /// Kullanıcının belirli bir role sahip olup olmadığını kontrol eder
        /// Authorization için kullanılır
        /// </summary>
        /// <param name="requiredRole">Gerekli rol</param>
        /// <returns>Yetki varsa true</returns>
        public bool HasRole(UserRole requiredRole)
        {
            if (!IsAuthenticated) return false;

            // Admin her yetki seviyesine erişebilir
            if (_currentUser!.Role == UserRole.Admin) return true;

            // Supervisör technician yetkilerine de sahiptir
            if (_currentUser.Role == UserRole.Supervisor && requiredRole == UserRole.Technician) return true;

            // Tam eşleşme kontrolü
            return _currentUser.Role == requiredRole;
        }

        /// <summary>
        /// Admin yetkisi kontrolü
        /// Sadece Admin rolündeki kullanıcılar için true döner
        /// </summary>
        public bool IsAdmin => IsAuthenticated && _currentUser!.Role == UserRole.Admin;

        /// <summary>
        /// Supervisör veya Admin yetkisi kontrolü
        /// </summary>
        public bool IsSupervisorOrAdmin => IsAuthenticated &&
            (_currentUser!.Role == UserRole.Supervisor || _currentUser.Role == UserRole.Admin);

        /// <summary>
        /// Giriş bilgilerini güvenli şekilde saklar
        /// </summary>
        private async Task SaveCredentialsAsync(string email, string password)
        {
            await _secureStorage.SetAsync(SAVED_EMAIL_KEY, email);
            await _secureStorage.SetAsync(SAVED_PASSWORD_KEY, password);
            await _secureStorage.SetAsync(REMEMBER_ME_KEY, "true");
        }

        /// <summary>
        /// Kaydedilmiş giriş bilgilerini temizler
        /// </summary>
        private async Task ClearSavedCredentialsAsync()
        {
            await _secureStorage.RemoveAsync(SAVED_EMAIL_KEY);
            await _secureStorage.RemoveAsync(SAVED_PASSWORD_KEY);
            await _secureStorage.RemoveAsync(REMEMBER_ME_KEY);
        }

        /// <summary>
        /// Tüm session bilgilerini temizler
        /// </summary>
        private async Task ClearSessionAsync()
        {
            _currentUser = null;
            await _secureStorage.RemoveAsync(CURRENT_USER_KEY);
        }
    }
}
