namespace CRM.Data.Repositories
{
    /// <summary>
    /// Customer repository implementation
    /// Müşteri yönetimi için özelleştirilmiş veri erişim operasyonları
    /// </summary>
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(TeknikServisDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Vergi numarasına göre müşteri getirir
        /// Müşteri kaydı sırasında duplicate kontrolü için kullanılır
        /// </summary>
        public async Task<Customer?> GetByTaxNumberAsync(string taxNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.TaxNumber == taxNumber);
        }

        /// <summary>
        /// Firma tipine göre müşterileri getirir
        /// Raporlama ve filtreleme için kullanılır
        /// </summary>
        public async Task<IEnumerable<Customer>> GetByCompanyTypeAsync(CompanyType companyType)
        {
            return await _dbSet
                .Where(c => c.CompanyType == companyType && c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
        }

        /// <summary>
        /// Aktif müşterileri getirir
        /// UI'da müşteri listelerinde sadece aktif müşterileri göstermek için
        /// </summary>
        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
        {
            return await _dbSet
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
        }

        /// <summary>
        /// Müşteri arama işlemi
        /// Firma adı, yetkili kişi adı, telefon numarası ve vergi numarasında arama yapar
        /// Servis formunda müşteri seçimi için kritik
        /// </summary>
        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveCustomersAsync();

            var term = searchTerm.ToLower();

            return await _dbSet
                .Where(c => c.IsActive &&
                           (c.CompanyName.ToLower().Contains(term) ||
                            c.AuthorizedPersonName.ToLower().Contains(term) ||
                            c.PhoneNumber.Contains(searchTerm) ||
                            c.TaxNumber.Contains(searchTerm)))
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
        }

        /// <summary>
        /// Vergi numarasının benzersiz olup olmadığını kontrol eder
        /// Müşteri kayıt/güncelleme işlemlerinde validation için
        /// </summary>
        public async Task<bool> IsTaxNumberUniqueAsync(string taxNumber, int? excludeCustomerId = null)
        {
            var query = _dbSet.Where(c => c.TaxNumber == taxNumber);

            if (excludeCustomerId.HasValue)
                query = query.Where(c => c.Id != excludeCustomerId.Value);

            return !await query.AnyAsync();
        }

        /// <summary>
        /// Müşterileri servis sayıları ile birlikte getirir
        /// Dashboard ve raporlama için kullanılır
        /// </summary>
        public async Task<(IEnumerable<Customer> customers, int totalCount)> GetCustomersWithServicesAsync(
            int pageNumber, int pageSize, string? searchTerm = null)
        {
            var query = _dbSet.Include(c => c.Services).AsQueryable();

            // Aktif müşteri filtresi
            query = query.Where(c => c.IsActive);

            // Arama terimi varsa filtrele
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(c => c.CompanyName.ToLower().Contains(term) ||
                                        c.AuthorizedPersonName.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            var customers = await query
                .OrderBy(c => c.CompanyName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (customers, totalCount);
        }

        /// <summary>
        /// Belirli bir müşteriyi tüm servisleri ile birlikte getirir
        /// Müşteri detay sayfası için kullanılır
        /// </summary>
        public async Task<Customer?> GetCustomerWithServicesAsync(int customerId)
        {
            return await _dbSet
                .Include(c => c.Services)
                    .ThenInclude(s => s.ServiceTasks)
                .Include(c => c.Services)
                    .ThenInclude(s => s.ServiceUsers)
                        .ThenInclude(su => su.User)
                .FirstOrDefaultAsync(c => c.Id == customerId);
        }

        /// <summary>
        /// En çok servisi olan müşterileri getirir
        /// Dashboard'da top müşteriler widget'ı için kullanılır
        /// </summary>
        public async Task<IEnumerable<Customer>> GetTopCustomersByServiceCountAsync(int count = 10)
        {
            return await _dbSet
                .Include(c => c.Services)
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.Services.Count)
                .Take(count)
                .ToListAsync();
        }
    }
}
