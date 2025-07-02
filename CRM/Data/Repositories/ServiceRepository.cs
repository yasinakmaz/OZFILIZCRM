namespace CRM.Data.Repositories
{
    /// <summary>
    /// Service repository implementation
    /// Servis yönetimi için özelleştirilmiş veri erişim operasyonları
    /// </summary>
    public class ServiceRepository : Repository<Service>, IServiceRepository
    {
        public ServiceRepository(TeknikServisDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Duruma göre servisleri getirir
        /// Farklı statüdeki servisleri listelemek için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetByStatusAsync(ServiceStatus status)
        {
            return await _dbSet
                .Include(s => s.Customer)
                .Include(s => s.CreatedByUser)
                .Where(s => s.Status == status)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir müşterinin tüm servislerini getirir
        /// Müşteri detay sayfasında servis geçmişi için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetByCustomerIdAsync(int customerId)
        {
            return await _dbSet
                .Include(s => s.ServiceTasks)
                .Include(s => s.ServiceUsers)
                    .ThenInclude(su => su.User)
                .Where(s => s.CustomerId == customerId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir kullanıcının atandığı servisleri getirir
        /// Teknisyen dashboard'ında kişisel servis listesi için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(s => s.Customer)
                .Include(s => s.ServiceTasks)
                .Include(s => s.ServiceUsers.Where(su => su.UserId == userId && su.IsActive))
                .Where(s => s.ServiceUsers.Any(su => su.UserId == userId && su.IsActive))
                .OrderByDescending(s => s.ScheduledDateTime)
                .ToListAsync();
        }

        /// <summary>
        /// Servisin tüm detayları ile birlikte getirir
        /// Servis detay sayfası ve düzenleme formu için kullanılır
        /// </summary>
        public async Task<Service?> GetServiceWithDetailsAsync(int serviceId)
        {
            return await _dbSet
                .Include(s => s.Customer)
                .Include(s => s.CreatedByUser)
                .Include(s => s.ApprovedByUser)
                .Include(s => s.ServiceTasks)
                    .ThenInclude(st => st.CompletedByUser)
                .Include(s => s.ServiceUsers.Where(su => su.IsActive))
                    .ThenInclude(su => su.User)
                .Include(s => s.ServiceUsers.Where(su => su.IsActive))
                    .ThenInclude(su => su.AssignedByUser)
                .FirstOrDefaultAsync(s => s.Id == serviceId);
        }

        /// <summary>
        /// Bekleyen servisleri getirir (henüz personel atanmamış)
        /// Ana servis listesi sayfasında kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetPendingServicesAsync()
        {
            return await GetByStatusAsync(ServiceStatus.Pending);
        }

        /// <summary>
        /// Aktif servisleri getirir (üzerinde çalışılan)
        /// Güncel iş yükünü görmek için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetActiveServicesAsync()
        {
            return await GetByStatusAsync(ServiceStatus.InProgress);
        }

        /// <summary>
        /// Admin onayı bekleyen servisleri getirir
        /// Admin panelinde onay bekleyen işler için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetServicesAwaitingApprovalAsync()
        {
            return await GetByStatusAsync(ServiceStatus.WaitingApproval);
        }

        /// <summary>
        /// Tamamlanmış ve onaylanmış servisleri getirir
        /// Geçmiş servisler ve raporlama için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetCompletedServicesAsync()
        {
            return await GetByStatusAsync(ServiceStatus.Completed);
        }

        /// <summary>
        /// Sayfalama ve filtreleme ile servisleri getirir
        /// Ana servis listesi sayfasında kullanılır
        /// </summary>
        public async Task<(IEnumerable<Service> services, int totalCount)> GetServicesPagedAsync(
            int pageNumber, int pageSize, ServiceStatus? status = null,
            int? customerId = null, string? searchTerm = null)
        {
            var query = _dbSet
                .Include(s => s.Customer)
                .Include(s => s.CreatedByUser)
                .Include(s => s.ServiceUsers.Where(su => su.IsActive))
                    .ThenInclude(su => su.User)
                .AsQueryable();

            // Status filtresi
            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            // Müşteri filtresi
            if (customerId.HasValue)
                query = query.Where(s => s.CustomerId == customerId.Value);

            // Arama terimi
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(s => s.Customer.CompanyName.ToLower().Contains(term) ||
                                        s.Notes != null && s.Notes.ToLower().Contains(term) ||
                                        s.Id.ToString().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var services = await query
                .OrderByDescending(s => s.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (services, totalCount);
        }

        /// <summary>
        /// Belirli tarih aralığındaki servisleri getirir
        /// Raporlama ve analiz için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetServicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(s => s.Customer)
                .Include(s => s.ServiceUsers)
                    .ThenInclude(su => su.User)
                .Where(s => s.CreatedDate >= startDate && s.CreatedDate <= endDate)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Her statüdeki servis sayılarını getirir
        /// Dashboard widget'ları için kullanılır
        /// </summary>
        public async Task<Dictionary<ServiceStatus, int>> GetServiceStatusCountsAsync()
        {
            return await _dbSet
                .GroupBy(s => s.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Vadesi geçmiş servisleri getirir
        /// Uyarı ve takip sistemleri için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetOverdueServicesAsync()
        {
            var now = DateTime.Now;

            return await _dbSet
                .Include(s => s.Customer)
                .Include(s => s.ServiceUsers)
                    .ThenInclude(su => su.User)
                .Where(s => s.ScheduledDateTime < now &&
                           (s.Status == ServiceStatus.Pending || s.Status == ServiceStatus.InProgress))
                .OrderBy(s => s.ScheduledDateTime)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli tarih aralığındaki toplam servis tutarını hesaplar
        /// Mali raporlama için kullanılır
        /// </summary>
        public async Task<decimal> GetTotalServiceAmountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(s => s.ServiceEndDateTime >= startDate &&
                           s.ServiceEndDateTime <= endDate &&
                           s.Status == ServiceStatus.Completed &&
                           s.ServiceAmount.HasValue)
                .SumAsync(s => s.ServiceAmount!.Value);
        }

        /// <summary>
        /// Belirli kullanıcının servislerini duruma göre getirir
        /// Kullanıcı bazlı performans raporları için kullanılır
        /// </summary>
        public async Task<IEnumerable<Service>> GetUserServicesAsync(int userId, ServiceStatus? status = null)
        {
            var query = _dbSet
                .Include(s => s.Customer)
                .Include(s => s.ServiceTasks)
                .Where(s => s.ServiceUsers.Any(su => su.UserId == userId && su.IsActive));

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            return await query
                .OrderByDescending(s => s.ScheduledDateTime)
                .ToListAsync();
        }
    }
}
