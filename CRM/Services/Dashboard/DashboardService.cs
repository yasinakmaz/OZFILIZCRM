namespace CRM.Services.Dashboard
{
    /// <summary>
    /// Dashboard servisi implementation
    /// Comprehensive dashboard analytics ve real-time data
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly IServiceTaskRepository _taskRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IServiceRepository serviceRepository,
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            IServiceTaskRepository taskRepository,
            ICacheService cacheService,
            ILogger<DashboardService> logger)
        {
            _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Dashboard'un tüm verilerini getirir
        /// </summary>
        public async Task<ServiceResult<DashboardDto>> GetDashboardDataAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Loading dashboard data for user: {UserId}", userId);

                // **CACHE KEY**
                var cacheKey = $"dashboard_{userId}_{DateTime.Now:yyyyMMddHH}"; // Hourly cache

                var cachedData = await _cacheService.GetAsync<DashboardDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Dashboard data loaded from cache");
                    return ServiceResult<DashboardDto>.Success(cachedData);
                }

                // **PARALLEL DATA LOADING** for performance
                var tasks = new[]
                {
                    GetServiceStatsAsync(),
                    GetCustomerStatsAsync(),
                    GetUserStatsAsync(),
                    GetRecentServicesAsync(10),
                    GetOverdueServicesAsync(),
                    GetTodayServicesAsync(),
                    GetTopCustomersAsync(),
                    GetActiveTechniciansAsync(),
                    GetRevenueChartDataAsync(6),
                    GetServiceStatusChartDataAsync()
                };

                await Task.WhenAll(tasks);

                // **BUILD DASHBOARD DTO**
                var dashboard = new DashboardDto
                {
                    ServiceStats = tasks[0].Result.Data ?? new ServiceStatsDto(),
                    CustomerStats = tasks[1].Result.Data ?? new CustomerStatsDto(),
                    UserStats = tasks[2].Result.Data ?? new UserStatsDto(),
                    RecentServices = tasks[3].Result.Data?.ToList() ?? new List<ServiceListDto>(),
                    OverdueServices = tasks[4].Result.Data?.ToList() ?? new List<ServiceListDto>(),
                    TodayServices = tasks[5].Result.Data?.ToList() ?? new List<ServiceListDto>(),
                    TopCustomers = tasks[6].Result.Data?.ToList() ?? new List<CustomerSearchDto>(),
                    ActiveTechnicians = tasks[7].Result.Data?.ToList() ?? new List<UserDto>(),
                    RevenueChart = tasks[8].Result.Data ?? new ChartDataDto(),
                    ServiceStatusChart = tasks[9].Result.Data ?? new ChartDataDto(),
                    LastUpdated = DateTime.Now
                };

                // **CACHE RESULT**
                await _cacheService.SetAsync(cacheKey, dashboard, TimeSpan.FromHours(1));

                _logger.LogInformation("Dashboard data loaded successfully for user: {UserId}", userId);

                return ServiceResult<DashboardDto>.Success(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data for user: {UserId}", userId);
                return ServiceResult<DashboardDto>.Failure("Dashboard verileri yüklenirken hata oluştu.");
            }
        }

        /// <summary>
        /// Servis istatistiklerini getirir
        /// </summary>
        public async Task<ServiceResult<ServiceStatsDto>> GetServiceStatsAsync()
        {
            try
            {
                var services = await _serviceRepository.GetAllAsync();
                var now = DateTime.Now;
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var yearStart = new DateTime(now.Year, 1, 1);

                var stats = new ServiceStatsDto
                {
                    TotalServices = services.Count(),
                    PendingServices = services.Count(s => s.Status == ServiceStatus.Pending),
                    InProgressServices = services.Count(s => s.Status == ServiceStatus.InProgress),
                    CompletedServices = services.Count(s => s.Status == ServiceStatus.Completed),
                    OverdueServices = services.Count(s => s.IsOverdue),
                    TodayServices = services.Count(s => s.ExpectedCompletionDate?.Date == DateTime.Today),
                    TotalRevenue = services.Where(s => s.ServiceAmount.HasValue).Sum(s => s.ServiceAmount!.Value),
                    MonthlyRevenue = services.Where(s => s.CreatedDate >= monthStart && s.ServiceAmount.HasValue)
                                           .Sum(s => s.ServiceAmount!.Value)
                };

                // **COMPLETION TIME CALCULATION**
                var completedServices = services.Where(s => s.Status == ServiceStatus.Completed &&
                                                         s.ServiceStartDateTime.HasValue &&
                                                         s.ServiceEndDateTime.HasValue);

                if (completedServices.Any())
                {
                    var completionTimes = completedServices.Select(s =>
                        (s.ServiceEndDateTime!.Value - s.ServiceStartDateTime!.Value).TotalDays);
                    stats.AverageCompletionDays = completionTimes.Average();
                }

                // **CUSTOMER SATISFACTION** (simulated - gerçek projede customer feedback'den alınır)
                stats.CustomerSatisfactionRate = 4.2; // Out of 5

                return ServiceResult<ServiceStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service stats");
                return ServiceResult<ServiceStatsDto>.Failure("Servis istatistikleri alınırken hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteri istatistiklerini getirir
        /// </summary>
        public async Task<ServiceResult<CustomerStatsDto>> GetCustomerStatsAsync()
        {
            try
            {
                var customers = await _customerRepository.GetAllAsync();

                var stats = new CustomerStatsDto
                {
                    TotalCustomers = customers.Count(),
                    ActiveCustomers = customers.Count(c => c.IsActive),
                    InactiveCustomers = customers.Count(c => !c.IsActive),
                    CorporateCustomers = customers.Count(c => c.CustomerType == CustomerType.Corporate),
                    IndividualCustomers = customers.Count(c => c.CustomerType == CustomerType.Individual),
                    DealerCustomers = customers.Count(c => c.CustomerType == CustomerType.Dealer)
                };

                // **CUSTOMERS WITH ACTIVE SERVICES**
                var customersWithServices = new List<int>();
                foreach (var customer in customers)
                {
                    var hasActiveServices = await _customerRepository.HasActiveServicesAsync(customer.Id);
                    if (hasActiveServices)
                        customersWithServices.Add(customer.Id);
                }
                stats.CustomersWithActiveServices = customersWithServices.Count;

                // **REVENUE CALCULATIONS**
                foreach (var customer in customers)
                {
                    var customerRevenue = await _customerRepository.GetTotalServiceAmountAsync(customer.Id);
                    stats.TotalServiceRevenue += customerRevenue;
                }

                if (stats.TotalCustomers > 0)
                {
                    stats.AverageServiceValue = stats.TotalServiceRevenue / stats.TotalCustomers;
                }

                return ServiceResult<CustomerStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer stats");
                return ServiceResult<CustomerStatsDto>.Failure("Müşteri istatistikleri alınırken hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcı istatistiklerini getirir
        /// </summary>
        public async Task<ServiceResult<UserStatsDto>> GetUserStatsAsync()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var now = DateTime.Now;

                var stats = new UserStatsDto
                {
                    TotalUsers = users.Count(),
                    ActiveUsers = users.Count(u => u.IsActive),
                    InactiveUsers = users.Count(u => !u.IsActive),
                    OnlineUsers = users.Count(u => u.LastLoginDate.HasValue &&
                                                  u.LastLoginDate.Value > now.AddMinutes(-30)),
                    AdminUsers = users.Count(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin),
                    TechnicianUsers = users.Count(u => u.Role == UserRole.Technician),
                    CustomerRepUsers = users.Count(u => u.Role == UserRole.CustomerRepresentative)
                };

                // **LAST USER REGISTRATION**
                var lastUser = users.OrderByDescending(u => u.CreatedDate).FirstOrDefault();
                if (lastUser != null)
                {
                    stats.LastUserRegistration = lastUser.CreatedDate;
                }

                // **MOST ACTIVE USER**
                var userActivities = new List<(User User, int ActivityCount)>();
                foreach (var user in users.Where(u => u.IsActive))
                {
                    var userServices = await _serviceRepository.GetServicesByUserAsync(user.Id);
                    var activityCount = userServices.Count() +
                                      (user.LastLoginDate.HasValue && user.LastLoginDate.Value > now.AddDays(-7) ? 10 : 0);
                    userActivities.Add((user, activityCount));
                }

                if (userActivities.Any())
                {
                    var mostActive = userActivities.OrderByDescending(ua => ua.ActivityCount).First();
                    stats.MostActiveUser = mostActive.User.FullName;
                }

                return ServiceResult<UserStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return ServiceResult<UserStatsDto>.Failure("Kullanıcı istatistikleri alınırken hata oluştu.");
            }
        }

        /// <summary>
        /// Son servisleri getirir
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ServiceListDto>>> GetRecentServicesAsync(int count = 10)
        {
            try
            {
                var services = await _serviceRepository.GetAllAsync(s => s.Customer, s => s.AssignedUser);
                var recentServices = services
                    .OrderByDescending(s => s.CreatedDate)
                    .Take(count)
                    .Select(MapToServiceListDto)
                    .ToList();

                return ServiceResult<IEnumerable<ServiceListDto>>.Success(recentServices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent services");
                return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Son servisler alınırken hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcının dashboard'ında gösterilecek servisleri getirir
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ServiceListDto>>> GetUserDashboardServicesAsync(int userId, int count = 5)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Kullanıcı bulunamadı.");
                }

                IEnumerable<Service> services;

                if (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
                {
                    // Admin'ler için tüm güncel servisler
                    var allServices = await _serviceRepository.GetAllAsync(s => s.Customer, s => s.AssignedUser);
                    services = allServices
                        .Where(s => s.Status == ServiceStatus.Pending || s.Status == ServiceStatus.InProgress)
                        .OrderByDescending(s => s.Priority)
                        .ThenBy(s => s.ExpectedCompletionDate)
                        .Take(count);
                }
                else
                {
                    // Teknisyenler için kendi servisleri
                    var userServices = await _serviceRepository.GetServicesByUserAsync(userId);
                    services = userServices
                        .Where(s => s.Status == ServiceStatus.Pending || s.Status == ServiceStatus.InProgress)
                        .OrderByDescending(s => s.Priority)
                        .ThenBy(s => s.ExpectedCompletionDate)
                        .Take(count);
                }

                var serviceDtos = services.Select(MapToServiceListDto).ToList();

                return ServiceResult<IEnumerable<ServiceListDto>>.Success(serviceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user dashboard services for user: {UserId}", userId);
                return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Kullanıcı servisleri alınırken hata oluştu.");
            }
        }

        /// <summary>
        /// Gelir grafiği verilerini getirir
        /// </summary>
        public async Task<ServiceResult<ChartDataDto>> GetRevenueChartDataAsync(int months = 6)
        {
            try
            {
                var chartData = new ChartDataDto
                {
                    Title = "Aylık Gelir Trendi",
                    Type = "line"
                };

                var startDate = DateTime.Now.AddMonths(-months);

                for (int i = months; i >= 0; i--)
                {
                    var monthStart = DateTime.Now.AddMonths(-i).Date;
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthlyRevenue = await _serviceRepository.GetTotalServiceAmountByPeriodAsync(monthStart, monthEnd);

                    chartData.Labels.Add(monthStart.ToString("MMM yyyy"));
                    chartData.Values.Add(monthlyRevenue);
                    chartData.Colors.Add("#007bff");
                }

                return ServiceResult<ChartDataDto>.Success(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue chart data");
                return ServiceResult<ChartDataDto>.Failure("Gelir grafik verileri alınırken hata oluştu.");
            }
        }

        /// <summary>
        /// Servis durum grafiği verilerini getirir
        /// </summary>
        public async Task<ServiceResult<ChartDataDto>> GetServiceStatusChartDataAsync()
        {
            try
            {
                var services = await _serviceRepository.GetAllAsync();

                var chartData = new ChartDataDto
                {
                    Title = "Servis Durumları",
                    Type = "doughnut"
                };

                var statusGroups = services.GroupBy(s => s.Status).ToList();

                foreach (var group in statusGroups)
                {
                    chartData.Labels.Add(GetStatusDisplayName(group.Key));
                    chartData.Values.Add(group.Count());
                    chartData.Colors.Add(GetStatusColor(group.Key));
                }

                return ServiceResult<ChartDataDto>.Success(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service status chart data");
                return ServiceResult<ChartDataDto>.Failure("Servis durum grafik verileri alınırken hata oluştu.");
            }
        }

        /// <summary>
        /// Aylık servis sayısı grafiği verilerini getirir
        /// </summary>
        public async Task<ServiceResult<ChartDataDto>> GetMonthlyServiceCountChartAsync(int months = 12)
        {
            try
            {
                var chartData = new ChartDataDto
                {
                    Title = "Aylık Servis Sayıları",
                    Type = "bar"
                };

                for (int i = months; i >= 0; i--)
                {
                    var monthStart = DateTime.Now.AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1);

                    var services = await _serviceRepository.GetAllAsync();
                    var monthlyCount = services.Count(s => s.CreatedDate >= monthStart && s.CreatedDate < monthEnd);

                    chartData.Labels.Add(monthStart.ToString("MMM yyyy"));
                    chartData.Values.Add(monthlyCount);
                    chartData.Colors.Add("#28a745");
                }

                return ServiceResult<ChartDataDto>.Success(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly service count chart data");
                return ServiceResult<ChartDataDto>.Failure("Aylık servis grafik verileri alınırken hata oluştu.");
            }
        }

        // **PRIVATE HELPER METHODS**

        private async Task<ServiceResult<IEnumerable<ServiceListDto>>> GetOverdueServicesAsync()
        {
            try
            {
                var services = await _serviceRepository.GetOverdueServicesAsync();
                var serviceDtos = services.Select(MapToServiceListDto).ToList();
                return ServiceResult<IEnumerable<ServiceListDto>>.Success(serviceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue services");
                return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Geciken servisler alınırken hata oluştu.");
            }
        }

        private async Task<ServiceResult<IEnumerable<ServiceListDto>>> GetTodayServicesAsync()
        {
            try
            {
                var services = await _serviceRepository.GetServicesDueTodayAsync();
                var serviceDtos = services.Select(MapToServiceListDto).ToList();
                return ServiceResult<IEnumerable<ServiceListDto>>.Success(serviceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today services");
                return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Bugünkü servisler alınırken hata oluştu.");
            }
        }

        private async Task<ServiceResult<IEnumerable<CustomerSearchDto>>> GetTopCustomersAsync()
        {
            try
            {
                var customers = await _customerRepository.GetActiveCustomersAsync();
                var topCustomers = new List<(Customer Customer, decimal Revenue)>();

                foreach (var customer in customers.Take(10)) // Limit for performance
                {
                    var revenue = await _customerRepository.GetTotalServiceAmountAsync(customer.Id);
                    topCustomers.Add((customer, revenue));
                }

                var topCustomerDtos = topCustomers
                    .OrderByDescending(tc => tc.Revenue)
                    .Take(5)
                    .Select(tc => new CustomerSearchDto
                    {
                        Id = tc.Customer.Id,
                        CompanyName = tc.Customer.CompanyName,
                        ContactPerson = tc.Customer.ContactPerson,
                        Email = tc.Customer.Email,
                        PhoneNumber = tc.Customer.PhoneNumber,
                        CustomerType = tc.Customer.CustomerType
                    })
                    .ToList();

                return ServiceResult<IEnumerable<CustomerSearchDto>>.Success(topCustomerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top customers");
                return ServiceResult<IEnumerable<CustomerSearchDto>>.Failure("Top müşteriler alınırken hata oluştu.");
            }
        }

        private async Task<ServiceResult<IEnumerable<UserDto>>> GetActiveTechniciansAsync()
        {
            try
            {
                var technicians = await _userRepository.GetByRoleAsync(UserRole.Technician);
                var activeTechnicians = technicians
                    .Where(t => t.IsActive)
                    .Take(5)
                    .Select(t => new UserDto
                    {
                        Id = t.Id,
                        Username = t.Username,
                        FirstName = t.FirstName,
                        LastName = t.LastName,
                        Email = t.Email,
                        Role = t.Role,
                        LastLoginDate = t.LastLoginDate
                    })
                    .ToList();

                return ServiceResult<IEnumerable<UserDto>>.Success(activeTechnicians);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active technicians");
                return ServiceResult<IEnumerable<UserDto>>.Failure("Aktif teknisyenler alınırken hata oluştu.");
            }
        }

        private static ServiceListDto MapToServiceListDto(Service service)
        {
            return new ServiceListDto
            {
                Id = service.Id,
                Title = service.Title,
                CustomerName = service.Customer?.CompanyName ?? "Bilinmeyen",
                AssignedUserName = service.AssignedUser?.FullName,
                Status = service.Status,
                Priority = service.Priority,
                ExpectedCompletionDate = service.ExpectedCompletionDate,
                ServiceAmount = service.ServiceAmount,
                CreatedDate = service.CreatedDate,
                IsOverdue = service.IsOverdue,
                ProgressPercentage = service.ProgressPercentage
            };
        }

        private static string GetStatusDisplayName(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Pending => "Beklemede",
                ServiceStatus.Accepted => "Kabul Edildi",
                ServiceStatus.InProgress => "Devam Ediyor",
                ServiceStatus.WaitingForParts => "Parça Bekleniyor",
                ServiceStatus.Testing => "Test Ediliyor",
                ServiceStatus.Completed => "Tamamlandı",
                ServiceStatus.Cancelled => "İptal Edildi",
                ServiceStatus.Rejected => "Reddedildi",
                _ => status.ToString()
            };
        }

        private static string GetStatusColor(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Pending => "#ffc107",      // Warning yellow
                ServiceStatus.Accepted => "#17a2b8",     // Info blue
                ServiceStatus.InProgress => "#007bff",   // Primary blue
                ServiceStatus.WaitingForParts => "#fd7e14", // Orange
                ServiceStatus.Testing => "#6f42c1",      // Purple
                ServiceStatus.Completed => "#28a745",    // Success green
                ServiceStatus.Cancelled => "#6c757d",    // Secondary gray
                ServiceStatus.Rejected => "#dc3545",     // Danger red
                _ => "#6c757d"
            };
        }
    }
}
