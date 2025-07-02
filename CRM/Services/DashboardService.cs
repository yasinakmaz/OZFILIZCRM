namespace CRM.Services
{
    /// <summary>
    /// Dashboard sayfası için gerekli veri ve istatistikleri sağlayan servis
    /// Performans odaklı tasarlanmış, cache-friendly metodlar içerir
    /// </summary>
    public class DashboardService
    {
        private readonly TeknikServisDbContext _context;
        private readonly IServiceRepository _serviceRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;

        public DashboardService(
            TeknikServisDbContext context,
            IServiceRepository serviceRepository,
            IUserRepository userRepository,
            ICustomerRepository customerRepository)
        {
            _context = context;
            _serviceRepository = serviceRepository;
            _userRepository = userRepository;
            _customerRepository = customerRepository;
        }

        /// <summary>
        /// Dashboard ana istatistiklerini getirir
        /// Tüm kullanıcı rolleri için temel metrikler
        /// </summary>
        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var stats = new DashboardStatsDto();

            // Paralel sorgu çalıştırarak performansı artır
            var tasks = new[]
            {
                Task.Run(async () => stats.TotalServices = await _context.Services.CountAsync()),
                Task.Run(async () => stats.TotalCustomers = await _context.Customers.CountAsync(c => c.IsActive)),
                Task.Run(async () => stats.TotalUsers = await _context.Users.CountAsync(u => u.IsActive)),
                Task.Run(async () => stats.PendingServices = await _context.Services.CountAsync(s => s.Status == ServiceStatus.Pending)),
                Task.Run(async () => stats.ActiveServices = await _context.Services.CountAsync(s => s.Status == ServiceStatus.InProgress)),
                Task.Run(async () => stats.CompletedServicesThisMonth = await GetCompletedServicesThisMonthAsync()),
                Task.Run(async () => stats.OverdueServices = await GetOverdueServicesCountAsync()),
                Task.Run(async () => stats.TotalRevenueThisMonth = await GetTotalRevenueThisMonthAsync())
            };

            await Task.WhenAll(tasks);

            return stats;
        }

        /// <summary>
        /// Kullanıcı bazlı performans istatistiklerini getirir
        /// Admin ve Supervisor'lar için team performance
        /// </summary>
        public async Task<IEnumerable<UserPerformanceDto>> GetUserPerformanceAsync(int? userId = null)
        {
            var startDate = DateTime.Now.AddMonths(-1);
            var endDate = DateTime.Now;

            var query = from su in _context.ServiceUsers
                        join service in _context.Services on su.ServiceId equals service.Id
                        join user in _context.Users on su.UserId equals user.Id
                        where su.IsActive &&
                              service.CreatedDate >= startDate &&
                              service.CreatedDate <= endDate
                        group new { su, service } by new { user.Id, user.Username, user.Email } into g
                        select new UserPerformanceDto
                        {
                            UserId = g.Key.Id,
                            Username = g.Key.Username,
                            Email = g.Key.Email,
                            TotalServices = g.Count(),
                            CompletedServices = g.Count(x => x.service.Status == ServiceStatus.Completed),
                            PendingServices = g.Count(x => x.service.Status == ServiceStatus.Pending),
                            ActiveServices = g.Count(x => x.service.Status == ServiceStatus.InProgress),
                            AverageCompletionDays = g.Where(x => x.service.ServiceEndDateTime.HasValue)
                                                    .Average(x => EF.Functions.DateDiffDay(x.service.ServiceStartDateTime!.Value, x.service.ServiceEndDateTime!.Value)) ?? 0
                        };

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            var results = await query.OrderByDescending(p => p.CompletedServices).ToListAsync();

            // Completion rate hesapla
            foreach (var result in results)
            {
                result.CompletionRate = result.TotalServices > 0
                    ? (double)result.CompletedServices / result.TotalServices * 100
                    : 0;
            }

            return results;
        }

        /// <summary>
        /// Son servisleri getirir
        /// Dashboard'da "Son İşlemler" widget'ı için kullanılır
        /// </summary>
        public async Task<IEnumerable<RecentServiceDto>> GetRecentServicesAsync(int count = 10, int? userId = null)
        {
            var query = _context.Services
                .Include(s => s.Customer)
                .Include(s => s.ServiceUsers.Where(su => su.IsActive))
                    .ThenInclude(su => su.User)
                .AsQueryable();

            // Eğer userId verilmişse, sadece o kullanıcının servislerini getir
            if (userId.HasValue)
                query = query.Where(s => s.ServiceUsers.Any(su => su.UserId == userId.Value && su.IsActive));

            var services = await query
                .OrderByDescending(s => s.CreatedDate)
                .Take(count)
                .Select(s => new RecentServiceDto
                {
                    ServiceId = s.Id,
                    CustomerName = s.Customer.CompanyName,
                    Status = s.Status,
                    CreatedDate = s.CreatedDate,
                    ScheduledDateTime = s.ScheduledDateTime,
                    ServiceAmount = s.ServiceAmount,
                    AssignedUsers = s.ServiceUsers.Where(su => su.IsActive).Select(su => su.User.Username).ToList()
                })
                .ToListAsync();

            return services;
        }

        /// <summary>
        /// Aylık servis trend verilerini getirir
        /// Dashboard'da grafik gösterimi için kullanılır
        /// </summary>
        public async Task<IEnumerable<MonthlyTrendDto>> GetMonthlyTrendsAsync(int monthCount = 12)
        {
            var startDate = DateTime.Now.AddMonths(-monthCount);

            var monthlyData = await _context.Services
                .Where(s => s.CreatedDate >= startDate)
                .GroupBy(s => new { s.CreatedDate.Year, s.CreatedDate.Month })
                .Select(g => new MonthlyTrendDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalServices = g.Count(),
                    CompletedServices = g.Count(s => s.Status == ServiceStatus.Completed),
                    TotalRevenue = g.Where(s => s.ServiceAmount.HasValue && s.Status == ServiceStatus.Completed)
                                   .Sum(s => s.ServiceAmount!.Value)
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            return monthlyData;
        }

        /// <summary>
        /// Status bazlı servis dağılımını getirir
        /// Pie chart widget'ı için kullanılır
        /// </summary>
        public async Task<Dictionary<ServiceStatus, int>> GetServiceStatusDistributionAsync(int? userId = null)
        {
            var query = _context.Services.AsQueryable();

            if (userId.HasValue)
                query = query.Where(s => s.ServiceUsers.Any(su => su.UserId == userId.Value && su.IsActive));

            return await query
                .GroupBy(s => s.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Top müşterileri servis sayısına göre getirir
        /// "En Çok Servis Alan Müşteriler" widget'ı için kullanılır
        /// </summary>
        public async Task<IEnumerable<TopCustomerDto>> GetTopCustomersAsync(int count = 5)
        {
            return await _context.Customers
                .Include(c => c.Services)
                .Where(c => c.IsActive)
                .Select(c => new TopCustomerDto
                {
                    CustomerId = c.Id,
                    CompanyName = c.CompanyName,
                    TotalServices = c.Services.Count(),
                    CompletedServices = c.Services.Count(s => s.Status == ServiceStatus.Completed),
                    TotalRevenue = c.Services.Where(s => s.ServiceAmount.HasValue && s.Status == ServiceStatus.Completed)
                                             .Sum(s => s.ServiceAmount!.Value),
                    LastServiceDate = c.Services.OrderByDescending(s => s.CreatedDate)
                                                .Select(s => s.CreatedDate)
                                                .FirstOrDefault()
                })
                .OrderByDescending(c => c.TotalServices)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Bekleyen görevleri getirir
        /// Kullanıcı bazlı to-do list widget'ı için kullanılır
        /// </summary>
        public async Task<IEnumerable<PendingTaskDto>> GetPendingTasksAsync(int userId, int count = 10)
        {
            return await _context.ServiceTasks
                .Include(st => st.Service)
                    .ThenInclude(s => s.Customer)
                .Where(st => !st.IsCompleted &&
                            st.Service.ServiceUsers.Any(su => su.UserId == userId && su.IsActive))
                .OrderBy(st => st.Priority)
                .ThenBy(st => st.CreatedDate)
                .Take(count)
                .Select(st => new PendingTaskDto
                {
                    TaskId = st.Id,
                    ServiceId = st.ServiceId,
                    CustomerName = st.Service.Customer.CompanyName,
                    Description = st.Description,
                    Priority = st.Priority,
                    CreatedDate = st.CreatedDate
                })
                .ToListAsync();
        }

        // Private helper metodlar
        private async Task<int> GetCompletedServicesThisMonthAsync()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            return await _context.Services
                .CountAsync(s => s.Status == ServiceStatus.Completed &&
                               s.ServiceEndDateTime >= startOfMonth &&
                               s.ServiceEndDateTime <= endOfMonth);
        }

        private async Task<int> GetOverdueServicesCountAsync()
        {
            var now = DateTime.Now;
            return await _context.Services
                .CountAsync(s => s.ScheduledDateTime < now &&
                               (s.Status == ServiceStatus.Pending || s.Status == ServiceStatus.InProgress));
        }

        private async Task<decimal> GetTotalRevenueThisMonthAsync()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            return await _context.Services
                .Where(s => s.Status == ServiceStatus.Completed &&
                           s.ServiceEndDateTime >= startOfMonth &&
                           s.ServiceEndDateTime <= endOfMonth &&
                           s.ServiceAmount.HasValue)
                .SumAsync(s => s.ServiceAmount!.Value);
        }
    }

    // DTO sınıfları

    /// <summary>
    /// Dashboard ana istatistikleri için DTO
    /// </summary>
    public class DashboardStatsDto
    {
        public int TotalServices { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalUsers { get; set; }
        public int PendingServices { get; set; }
        public int ActiveServices { get; set; }
        public int CompletedServicesThisMonth { get; set; }
        public int OverdueServices { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
    }

    /// <summary>
    /// Kullanıcı performans metrikleri için DTO
    /// </summary>
    public class UserPerformanceDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int CompletedServices { get; set; }
        public int PendingServices { get; set; }
        public int ActiveServices { get; set; }
        public double CompletionRate { get; set; }
        public double AverageCompletionDays { get; set; }
    }

    /// <summary>
    /// Son servisler listesi için DTO
    /// </summary>
    public class RecentServiceDto
    {
        public int ServiceId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public ServiceStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public decimal? ServiceAmount { get; set; }
        public List<string> AssignedUsers { get; set; } = new List<string>();
    }

    /// <summary>
    /// Aylık trend verileri için DTO
    /// </summary>
    public class MonthlyTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalServices { get; set; }
        public int CompletedServices { get; set; }
        public decimal TotalRevenue { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    /// <summary>
    /// Top müşteriler için DTO
    /// </summary>
    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int TotalServices { get; set; }
        public int CompletedServices { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime? LastServiceDate { get; set; }
    }

    /// <summary>
    /// Bekleyen görevler için DTO
    /// </summary>
    public class PendingTaskDto
    {
        public int TaskId { get; set; }
        public int ServiceId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
