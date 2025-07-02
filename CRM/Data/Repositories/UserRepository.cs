namespace CRM.Data.Repositories
{
    /// <summary>
    /// User repository implementation
    /// Kullanıcı yönetimi için özelleştirilmiş veri erişim operasyonları
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(TeknikServisDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Email adresine göre kullanıcı getirir
        /// Login işlemlerinde kullanılır
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Kullanıcı adına göre kullanıcı getirir
        /// Alternatif login yöntemi için kullanılabilir
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <summary>
        /// Belirli role sahip kullanıcıları getirir
        /// Yetki bazlı listeleme için kullanılır
        /// </summary>
        public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
        {
            return await _dbSet.Where(u => u.Role == role && u.IsActive).ToListAsync();
        }

        /// <summary>
        /// Aktif kullanıcıları getirir
        /// UI'da kullanıcı listelerinde pasif kullanıcıları gizlemek için
        /// </summary>
        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _dbSet.Where(u => u.IsActive).OrderBy(u => u.Username).ToListAsync();
        }

        /// <summary>
        /// Email adresinin benzersiz olup olmadığını kontrol eder
        /// Kullanıcı kayıt/güncelleme işlemlerinde validation için
        /// </summary>
        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null)
        {
            var query = _dbSet.Where(u => u.Email == email);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return !await query.AnyAsync();
        }

        /// <summary>
        /// Kullanıcı adının benzersiz olup olmadığını kontrol eder
        /// </summary>
        public async Task<bool> IsUsernameUniqueAsync(string username, int? excludeUserId = null)
        {
            var query = _dbSet.Where(u => u.Username == username);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return !await query.AnyAsync();
        }

        /// <summary>
        /// Kullanıcı arama işlemi
        /// Ad, email veya username'de arama yapar
        /// </summary>
        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveUsersAsync();

            var term = searchTerm.ToLower();

            return await _dbSet
                .Where(u => u.IsActive &&
                           (u.Username.ToLower().Contains(term) ||
                            u.Email.ToLower().Contains(term)))
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        /// <summary>
        /// Servise atanabilir teknisyenleri getirir
        /// Sadece aktif teknisyen ve süpervizörleri listeler
        /// </summary>
        public async Task<IEnumerable<User>> GetAvailableTechniciansAsync()
        {
            return await _dbSet
                .Where(u => u.IsActive &&
                           (u.Role == UserRole.Technician || u.Role == UserRole.Supervisor))
                .OrderBy(u => u.Username)
                .ToListAsync();
        }
    }
}
