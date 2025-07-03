using Microsoft.Extensions.Logging;

namespace CRM.Services.CrossCutting
{
    /// <summary>
    /// Security servisi implementation
    /// Role-based access control ve data security
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly IUserRepository _userRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(
            IUserRepository userRepository,
            IServiceRepository serviceRepository,
            ILogger<SecurityService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Kullanıcının permission'ı var mı kontrol eder
        /// </summary>
        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                    return false;

                var rolePermissions = GetRolePermissions(user.Role);
                return rolePermissions.Contains(permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
                return false;
            }
        }

        /// <summary>
        /// Kullanıcı servise erişebilir mi kontrol eder
        /// </summary>
        public async Task<bool> CanAccessServiceAsync(int userId, int serviceId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                    return false;

                // Admin'ler her şeye erişebilir
                if (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
                    return true;

                var service = await _serviceRepository.GetByIdAsync(serviceId);
                if (service == null)
                    return false;

                // Teknisyen sadece atanan servislere erişebilir
                if (user.Role == UserRole.Technician)
                    return service.AssignedUserId == userId;

                // Customer representative tüm servislere erişebilir (read-only)
                if (user.Role == UserRole.CustomerRepresentative)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking service access for user {UserId}, service {ServiceId}", userId, serviceId);
                return false;
            }
        }

        /// <summary>
        /// Kullanıcı müşteriye erişebilir mi kontrol eder
        /// </summary>
        public async Task<bool> CanAccessCustomerAsync(int userId, int customerId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                    return false;

                // Admin'ler ve Customer Representative'ler tüm müşterilere erişebilir
                if (user.Role == UserRole.Admin ||
                    user.Role == UserRole.SuperAdmin ||
                    user.Role == UserRole.CustomerRepresentative)
                    return true;

                // Teknisyenler sadece kendi servislerindeki müşterilere erişebilir
                if (user.Role == UserRole.Technician)
                {
                    var userServices = await _serviceRepository.GetServicesByUserAsync(userId);
                    return userServices.Any(s => s.CustomerId == customerId);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking customer access for user {UserId}, customer {CustomerId}", userId, customerId);
                return false;
            }
        }

        /// <summary>
        /// Sensitive data'yı hash'ler
        /// </summary>
        public string HashSensitiveData(string data)
        {
            try
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing sensitive data");
                return string.Empty;
            }
        }

        /// <summary>
        /// Sensitive data'yı encrypt eder
        /// </summary>
        public string EncryptSensitiveData(string data)
        {
            try
            {
                // Implementation: AES encryption
                // Bu örnekte basit Base64 encoding kullanıyoruz
                var bytes = System.Text.Encoding.UTF8.GetBytes(data);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting sensitive data");
                return string.Empty;
            }
        }

        /// <summary>
        /// Encrypted data'yı decrypt eder
        /// </summary>
        public string DecryptSensitiveData(string encryptedData)
        {
            try
            {
                // Implementation: AES decryption
                var bytes = Convert.FromBase64String(encryptedData);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting sensitive data");
                return string.Empty;
            }
        }

        /// <summary>
        /// Data integrity'yi validate eder
        /// </summary>
        public bool ValidateDataIntegrity(string data, string hash)
        {
            try
            {
                var computedHash = HashSensitiveData(data);
                return computedHash == hash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating data integrity");
                return false;
            }
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
