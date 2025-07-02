namespace CRM.Services
{
    /// <summary>
    /// Kullanıcı yönetimi için business logic servisi
    /// CRUD işlemleri, şifre yönetimi ve yetki kontrolü
    /// </summary>
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly LoggingService _loggingService;

        public UserService(IUserRepository userRepository, LoggingService loggingService)
        {
            _userRepository = userRepository;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Sayfalama ile kullanıcı listesini getirir
        /// Arama ve filtreleme desteği vardır
        /// </summary>
        public async Task<(IEnumerable<User> users, int totalCount)> GetUsersPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            UserRole? role = null,
            bool includeInactive = false)
        {
            try
            {
                var predicate = includeInactive ? null : (System.Linq.Expressions.Expression<Func<User, bool>>)(u => u.IsActive);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchResults = await _userRepository.SearchUsersAsync(searchTerm);
                    var filteredResults = searchResults;

                    if (role.HasValue)
                        filteredResults = filteredResults.Where(u => u.Role == role.Value);

                    if (!includeInactive)
                        filteredResults = filteredResults.Where(u => u.IsActive);

                    var totalCount = filteredResults.Count();
                    var pagedResults = filteredResults
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    return (pagedResults, totalCount);
                }

                if (role.HasValue)
                {
                    var roleResults = await _userRepository.GetByRoleAsync(role.Value);
                    var totalCount = roleResults.Count();
                    var pagedResults = roleResults
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    return (pagedResults, totalCount);
                }

                return await _userRepository.GetPagedAsync(
                    pageNumber, pageSize, predicate,
                    orderBy: u => u.Username);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GET_USERS_PAGED", "User");
                throw;
            }
        }

        /// <summary>
        /// ID'ye göre kullanıcı getirir
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _userRepository.GetByIdAsync(userId);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GET_USER_BY_ID", "User", userId);
                throw;
            }
        }

        /// <summary>
        /// Kullanıcı arama işlemi
        /// Username ve email'de arama yapar
        /// </summary>
        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                return await _userRepository.SearchUsersAsync(searchTerm);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "SEARCH_USERS", "User");
                throw;
            }
        }

        /// <summary>
        /// Servise atanabilir teknisyenleri getirir
        /// </summary>
        public async Task<IEnumerable<User>> GetAvailableTechniciansAsync()
        {
            try
            {
                return await _userRepository.GetAvailableTechniciansAsync();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GET_AVAILABLE_TECHNICIANS", "User");
                throw;
            }
        }

        /// <summary>
        /// Yeni kullanıcı oluşturur
        /// Validation ve business rules kontrolü yapar
        /// </summary>
        public async Task<ServiceResult<User>> CreateUserAsync(UserDto userDto, int createdByUserId)
        {
            try
            {
                // Validation kontrolü
                var validationResult = await ValidateUserAsync(userDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<User>.Failure(validationResult.ErrorMessage!);
                }

                // Email benzersizlik kontrolü
                var isEmailUnique = await _userRepository.IsEmailUniqueAsync(userDto.Email);
                if (!isEmailUnique)
                {
                    return ServiceResult<User>.Failure("Bu email adresi ile kayıtlı bir kullanıcı zaten mevcut.");
                }

                // Username benzersizlik kontrolü
                var isUsernameUnique = await _userRepository.IsUsernameUniqueAsync(userDto.Username);
                if (!isUsernameUnique)
                {
                    return ServiceResult<User>.Failure("Bu kullanıcı adı zaten kullanılıyor.");
                }

                // Şifreyi hash'le
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

                // Entity oluştur
                var user = new User
                {
                    Username = userDto.Username.Trim(),
                    Email = userDto.Email.Trim().ToLower(),
                    PasswordHash = passwordHash,
                    Role = userDto.Role,
                    ProfileImage = userDto.ProfileImage,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                var createdUser = await _userRepository.AddAsync(user);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "CREATE_USER",
                    "User",
                    createdUser.Id,
                    $"Yeni kullanıcı oluşturuldu: {createdUser.Username} ({createdUser.Role})",
                    userId: createdByUserId,
                    newValues: new
                    {
                        createdUser.Username,
                        createdUser.Email,
                        createdUser.Role,
                        createdUser.IsActive
                    });

                return ServiceResult<User>.Success(createdUser);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "CREATE_USER", "User", userId: createdByUserId);
                return ServiceResult<User>.Failure("Kullanıcı oluşturulurken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcı bilgilerini günceller
        /// Şifre güncellemesi ayrı metodla yapılır
        /// </summary>
        public async Task<ServiceResult<User>> UpdateUserAsync(int userId, UserDto userDto, int updatedByUserId)
        {
            try
            {
                var existingUser = await _userRepository.GetByIdAsync(userId);
                if (existingUser == null)
                {
                    return ServiceResult<User>.Failure("Kullanıcı bulunamadı.");
                }

                // Validation kontrolü
                var validationResult = await ValidateUserAsync(userDto, userId);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<User>.Failure(validationResult.ErrorMessage!);
                }

                // Email benzersizlik kontrolü (mevcut kullanıcı hariç)
                var isEmailUnique = await _userRepository.IsEmailUniqueAsync(userDto.Email, userId);
                if (!isEmailUnique)
                {
                    return ServiceResult<User>.Failure("Bu email adresi ile kayıtlı başka bir kullanıcı mevcut.");
                }

                // Username benzersizlik kontrolü (mevcut kullanıcı hariç)
                var isUsernameUnique = await _userRepository.IsUsernameUniqueAsync(userDto.Username, userId);
                if (!isUsernameUnique)
                {
                    return ServiceResult<User>.Failure("Bu kullanıcı adı başka bir kullanıcı tarafından kullanılıyor.");
                }

                // Eski değerleri sakla (audit log için)
                var oldValues = new
                {
                    existingUser.Username,
                    existingUser.Email,
                    existingUser.Role,
                    existingUser.IsActive
                };

                // Güncelleme işlemi
                existingUser.Username = userDto.Username.Trim();
                existingUser.Email = userDto.Email.Trim().ToLower();
                existingUser.Role = userDto.Role;
                existingUser.IsActive = userDto.IsActive;
                existingUser.UpdatedDate = DateTime.Now;

                // Profil resmi güncellenirse
                if (!string.IsNullOrEmpty(userDto.ProfileImage))
                {
                    existingUser.ProfileImage = userDto.ProfileImage;
                }

                var updatedUser = await _userRepository.UpdateAsync(existingUser);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "UPDATE_USER",
                    "User",
                    userId,
                    $"Kullanıcı güncellendi: {updatedUser.Username}",
                    userId: updatedByUserId,
                    oldValues: oldValues,
                    newValues: new
                    {
                        updatedUser.Username,
                        updatedUser.Email,
                        updatedUser.Role,
                        updatedUser.IsActive
                    });

                return ServiceResult<User>.Success(updatedUser);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "UPDATE_USER", "User", userId, userId: updatedByUserId);
                return ServiceResult<User>.Failure("Kullanıcı güncellenirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcı şifresini değiştirir
        /// Eski şifre kontrolü ile güvenli güncelleme
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword,
            int changedByUserId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure("Kullanıcı bulunamadı.");
                }

                // Mevcut şifre kontrolü
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    return ServiceResult<bool>.Failure("Mevcut şifre yanlış.");
                }

                // Yeni şifre validation
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    return ServiceResult<bool>.Failure("Yeni şifre en az 6 karakter olmalıdır.");
                }

                // Yeni şifreyi hash'le ve güncelle
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedDate = DateTime.Now;

                await _userRepository.UpdateAsync(user);

                // İşlemi logla (şifre detayları loglanmaz)
                await _loggingService.LogAsync(
                    "CHANGE_PASSWORD",
                    "User",
                    userId,
                    $"Kullanıcı şifresi değiştirildi: {user.Username}",
                    userId: changedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "CHANGE_PASSWORD", "User", userId, userId: changedByUserId);
                return ServiceResult<bool>.Failure("Şifre değiştirilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcıyı pasif hale getirir (soft delete)
        /// Aktif servisleri olan kullanıcılar pasif yapılamaz
        /// </summary>
        public async Task<ServiceResult<bool>> DeactivateUserAsync(int userId, int deactivatedByUserId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure("Kullanıcı bulunamadı.");
                }

                // Son admin kontrolü - sistemde en az bir aktif admin kalmalı
                if (user.Role == UserRole.Admin)
                {
                    var activeAdmins = await _userRepository.GetByRoleAsync(UserRole.Admin);
                    if (activeAdmins.Count() <= 1)
                    {
                        return ServiceResult<bool>.Failure("Sistemde en az bir aktif admin bulunmalıdır.");
                    }
                }

                // TODO: Aktif servis kontrolü burada yapılabilir

                user.IsActive = false;
                user.UpdatedDate = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "DEACTIVATE_USER",
                    "User",
                    userId,
                    $"Kullanıcı pasif hale getirildi: {user.Username}",
                    userId: deactivatedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "DEACTIVATE_USER", "User", userId, userId: deactivatedByUserId);
                return ServiceResult<bool>.Failure("Kullanıcı pasif hale getirilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcıyı tekrar aktif hale getirir
        /// </summary>
        public async Task<ServiceResult<bool>> ActivateUserAsync(int userId, int activatedByUserId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure("Kullanıcı bulunamadı.");
                }

                user.IsActive = true;
                user.UpdatedDate = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "ACTIVATE_USER",
                    "User",
                    userId,
                    $"Kullanıcı aktif hale getirildi: {user.Username}",
                    userId: activatedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "ACTIVATE_USER", "User", userId, userId: activatedByUserId);
                return ServiceResult<bool>.Failure("Kullanıcı aktif hale getirilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcının profil resmini günceller
        /// Base64 string formatında kabul eder
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateProfileImageAsync(int userId, string? profileImageBase64, int updatedByUserId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure("Kullanıcı bulunamadı.");
                }

                user.ProfileImage = profileImageBase64;
                user.UpdatedDate = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "UPDATE_PROFILE_IMAGE",
                    "User",
                    userId,
                    $"Profil resmi güncellendi: {user.Username}",
                    userId: updatedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "UPDATE_PROFILE_IMAGE", "User", userId, userId: updatedByUserId);
                return ServiceResult<bool>.Failure("Profil resmi güncellenirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcı validation işlemi
        /// Business rules ve data validation kontrolü
        /// </summary>
        private async Task<ServiceResult<bool>> ValidateUserAsync(UserDto userDto, int? excludeUserId = null)
        {
            // Required field kontrolü
            if (string.IsNullOrWhiteSpace(userDto.Username))
                return ServiceResult<bool>.Failure("Kullanıcı adı gereklidir.");

            if (string.IsNullOrWhiteSpace(userDto.Email))
                return ServiceResult<bool>.Failure("Email adresi gereklidir.");

            if (!excludeUserId.HasValue && string.IsNullOrWhiteSpace(userDto.Password))
                return ServiceResult<bool>.Failure("Şifre gereklidir.");

            // Username format kontrolü
            if (userDto.Username.Length < 3)
                return ServiceResult<bool>.Failure("Kullanıcı adı en az 3 karakter olmalıdır.");

            if (userDto.Username.Length > 50)
                return ServiceResult<bool>.Failure("Kullanıcı adı en fazla 50 karakter olabilir.");

            // Email format kontrolü
            if (!IsValidEmail(userDto.Email))
                return ServiceResult<bool>.Failure("Geçerli bir email adresi giriniz.");

            // Şifre format kontrolü (yeni kullanıcı için)
            if (!excludeUserId.HasValue && !string.IsNullOrWhiteSpace(userDto.Password))
            {
                if (userDto.Password.Length < 6)
                    return ServiceResult<bool>.Failure("Şifre en az 6 karakter olmalıdır.");

                if (userDto.Password.Length > 100)
                    return ServiceResult<bool>.Failure("Şifre en fazla 100 karakter olabilir.");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// Email format kontrolü
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
