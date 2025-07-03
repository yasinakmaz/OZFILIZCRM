namespace CRM.Services.ServiceManagement
{
    /// <summary>
    /// Servis yönetimi interface'i
    /// Teknik servis iş süreçlerinin core business logic'i
    /// </summary>
    public interface IServiceManagementService
    {
        // **SERVICE CRUD OPERATIONS**
        Task<ServiceResult<ServiceDto>> CreateServiceAsync(ServiceDto serviceDto, int createdByUserId);
        Task<ServiceResult<ServiceDto>> UpdateServiceAsync(ServiceDto serviceDto, int updatedByUserId);
        Task<ServiceResult<bool>> DeleteServiceAsync(int serviceId, int deletedByUserId);
        Task<ServiceResult<ServiceDto>> GetServiceByIdAsync(int serviceId);
        Task<ServiceResult<PagedResultDto<ServiceListDto>>> GetServicesPagedAsync(
            int pageNumber, int pageSize,
            ServiceStatus? status = null,
            int? customerId = null,
            int? assignedUserId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        // **SERVICE STATUS MANAGEMENT**
        Task<ServiceResult<bool>> ChangeServiceStatusAsync(int serviceId, ServiceStatus newStatus, int changedByUserId, string? notes = null);
        Task<ServiceResult<bool>> AssignServiceAsync(int serviceId, int assignedUserId, int assignedByUserId);
        Task<ServiceResult<bool>> UnassignServiceAsync(int serviceId, int unassignedByUserId);
        Task<ServiceResult<bool>> CompleteServiceAsync(int serviceId, string solutionDescription, int completedByUserId);
        Task<ServiceResult<bool>> CancelServiceAsync(int serviceId, string reason, int cancelledByUserId);

        // **SERVICE TASK MANAGEMENT**
        Task<ServiceResult<ServiceTaskDto>> AddTaskToServiceAsync(int serviceId, ServiceTaskDto taskDto, int createdByUserId);
        Task<ServiceResult<bool>> CompleteTaskAsync(int taskId, int completedByUserId, string? notes = null);
        Task<ServiceResult<bool>> UpdateTaskAsync(ServiceTaskDto taskDto, int updatedByUserId);
        Task<ServiceResult<bool>> DeleteTaskAsync(int taskId, int deletedByUserId);
        Task<ServiceResult<IEnumerable<ServiceTaskDto>>> GetServiceTasksAsync(int serviceId);

        // **SERVICE QUERIES**
        Task<ServiceResult<IEnumerable<ServiceListDto>>> GetPendingServicesAsync();
        Task<ServiceResult<IEnumerable<ServiceListDto>>> GetMyServicesAsync(int userId);
        Task<ServiceResult<IEnumerable<ServiceListDto>>> GetOverdueServicesAsync();
        Task<ServiceResult<IEnumerable<ServiceListDto>>> GetTodayServicesAsync();
        Task<ServiceResult<ServiceStatsDto>> GetServiceStatsAsync();
        Task<ServiceResult<ServiceStatsDto>> GetUserServiceStatsAsync(int userId);

        // **ADVANCED FEATURES**
        Task<ServiceResult<bool>> SetServicePriorityAsync(int serviceId, Priority priority, int changedByUserId);
        Task<ServiceResult<bool>> UpdateServiceAmountAsync(int serviceId, decimal amount, int updatedByUserId);
        Task<ServiceResult<bool>> AddServiceNotesAsync(int serviceId, string notes, int addedByUserId, bool isCustomerNote = false);
        Task<ServiceResult<double>> CalculateServiceProgressAsync(int serviceId);
    }

    /// <summary>
    /// Servis yönetimi implementation
    /// Complex business rules ve workflow management
    /// </summary>
    public class ServiceManagementService : IServiceManagementService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IServiceTaskRepository _taskRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILoggingService _loggingService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ServiceManagementService> _logger;

        public ServiceManagementService(
            IServiceRepository serviceRepository,
            IServiceTaskRepository taskRepository,
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            ILoggingService loggingService,
            INotificationService notificationService,
            ILogger<ServiceManagementService> logger)
        {
            _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Yeni servis oluşturur
        /// Comprehensive validation ve business rules ile
        /// </summary>
        public async Task<ServiceResult<ServiceDto>> CreateServiceAsync(ServiceDto serviceDto, int createdByUserId)
        {
            try
            {
                // **VALIDATION**
                var validationResult = await ValidateServiceDataAsync(serviceDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<ServiceDto>.Failure(validationResult.ErrorMessage!);
                }

                // **BUSINESS RULES**
                var customer = await _customerRepository.GetByIdAsync(serviceDto.CustomerId);
                if (customer == null)
                {
                    return ServiceResult<ServiceDto>.Failure("Müşteri bulunamadı.");
                }

                if (!customer.IsActive)
                {
                    return ServiceResult<ServiceDto>.Failure("Pasif müşteriler için servis oluşturulamaz.");
                }

                // **AUTO-ASSIGN LOGIC**
                int? autoAssignedUserId = null;
                if (serviceDto.AssignedUserId == null && serviceDto.Priority >= Priority.High)
                {
                    autoAssignedUserId = await FindAvailableTechnicianAsync();
                }

                // **CREATE SERVICE ENTITY**
                var service = new Service
                {
                    Title = serviceDto.Title.Trim(),
                    Description = serviceDto.Description.Trim(),
                    CustomerId = serviceDto.CustomerId,
                    AssignedUserId = serviceDto.AssignedUserId ?? autoAssignedUserId,
                    Status = ServiceStatus.Pending,
                    Priority = serviceDto.Priority,
                    ServiceStartDateTime = serviceDto.ServiceStartDateTime,
                    ServiceEndDateTime = serviceDto.ServiceEndDateTime,
                    ExpectedCompletionDate = serviceDto.ExpectedCompletionDate ?? CalculateExpectedDate(serviceDto.Priority),
                    ServiceAmount = serviceDto.ServiceAmount,
                    IsWarrantyService = serviceDto.IsWarrantyService,
                    DeviceModel = serviceDto.DeviceModel?.Trim(),
                    DeviceSerialNumber = serviceDto.DeviceSerialNumber?.Trim(),
                    ProblemDescription = serviceDto.ProblemDescription?.Trim(),
                    TechnicianNotes = serviceDto.TechnicianNotes?.Trim(),
                    CustomerNotes = serviceDto.CustomerNotes?.Trim(),
                    CreatedDate = DateTime.Now
                };

                await _serviceRepository.AddAsync(service);
                await _serviceRepository.SaveChangesAsync();

                // **ADD INITIAL TASKS**
                if (serviceDto.Tasks?.Any() == true)
                {
                    foreach (var taskDto in serviceDto.Tasks)
                    {
                        var task = new ServiceTask
                        {
                            ServiceId = service.Id,
                            Description = taskDto.Description.Trim(),
                            Priority = taskDto.Priority,
                            EstimatedMinutes = 60,
                            CreatedDate = DateTime.Now
                        };
                        await _taskRepository.AddAsync(task);
                    }
                    await _taskRepository.SaveChangesAsync();
                }

                // **NOTIFICATIONS**
                await SendServiceCreatedNotificationsAsync(service, createdByUserId);

                // **AUDIT LOG**
                await _loggingService.LogAsync("CREATE_SERVICE", "Service", service.Id,
                    $"Yeni servis oluşturuldu: {service.Title} (Müşteri: {customer.CompanyName})",
                    userId: createdByUserId);

                // **RETURN DTO**
                var resultDto = await MapToServiceDtoAsync(service);

                _logger.LogInformation("Service created: {ServiceTitle} for customer {CustomerName} by user {UserId}",
                    service.Title, customer.CompanyName, createdByUserId);

                return ServiceResult<ServiceDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service");
                await _loggingService.LogErrorAsync(ex, "CREATE_SERVICE_ERROR", "Service", userId: createdByUserId);
                return ServiceResult<ServiceDto>.Failure("Servis oluşturma sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Servis bilgilerini günceller
        /// Status değişikliği business rules ile kontrol edilir
        /// </summary>
        public async Task<ServiceResult<ServiceDto>> UpdateServiceAsync(ServiceDto serviceDto, int updatedByUserId)
        {
            try
            {
                var service = await _serviceRepository.GetServiceWithDetailsAsync(serviceDto.Id);
                if (service == null)
                {
                    return ServiceResult<ServiceDto>.Failure("Servis bulunamadı.");
                }

                // **BUSINESS RULES**
                if (service.Status == ServiceStatus.Completed)
                {
                    return ServiceResult<ServiceDto>.Failure("Tamamlanmış servisler düzenlenemez.");
                }

                if (service.Status == ServiceStatus.Cancelled)
                {
                    return ServiceResult<ServiceDto>.Failure("İptal edilmiş servisler düzenlenemez.");
                }

                // **PERMISSION CHECK**
                var canEdit = await CanUserEditServiceAsync(service, updatedByUserId);
                if (!canEdit)
                {
                    return ServiceResult<ServiceDto>.Failure("Bu servisi düzenleme yetkiniz yok.");
                }

                // **TRACK CHANGES**
                var changes = new List<string>();

                if (service.Title != serviceDto.Title)
                {
                    changes.Add($"Başlık: '{service.Title}' → '{serviceDto.Title}'");
                    service.Title = serviceDto.Title.Trim();
                }

                if (service.Description != serviceDto.Description)
                {
                    changes.Add($"Açıklama güncellendi");
                    service.Description = serviceDto.Description.Trim();
                }

                if (service.Priority != serviceDto.Priority)
                {
                    changes.Add($"Öncelik: {service.Priority} → {serviceDto.Priority}");
                    service.Priority = serviceDto.Priority;
                }

                if (service.AssignedUserId != serviceDto.AssignedUserId)
                {
                    var oldUser = service.AssignedUserId.HasValue ?
                        (await _userRepository.GetByIdAsync(service.AssignedUserId.Value))?.FullName : "Atanmamış";
                    var newUser = serviceDto.AssignedUserId.HasValue ?
                        (await _userRepository.GetByIdAsync(serviceDto.AssignedUserId.Value))?.FullName : "Atanmamış";

                    changes.Add($"Atanan: {oldUser} → {newUser}");
                    service.AssignedUserId = serviceDto.AssignedUserId;
                }

                // **UPDATE OTHER FIELDS**
                service.ExpectedCompletionDate = serviceDto.ExpectedCompletionDate;
                service.ServiceAmount = serviceDto.ServiceAmount;
                service.DeviceModel = serviceDto.DeviceModel?.Trim();
                service.DeviceSerialNumber = serviceDto.DeviceSerialNumber?.Trim();
                service.ProblemDescription = serviceDto.ProblemDescription?.Trim();
                service.TechnicianNotes = serviceDto.TechnicianNotes?.Trim();
                service.CustomerNotes = serviceDto.CustomerNotes?.Trim();
                service.UpdatedDate = DateTime.Now;

                await _serviceRepository.UpdateAsync(service);
                await _serviceRepository.SaveChangesAsync();

                // **NOTIFICATIONS FOR ASSIGNMENT CHANGES**
                if (service.AssignedUserId != serviceDto.AssignedUserId && serviceDto.AssignedUserId.HasValue)
                {
                    await _notificationService.SendServiceAssignedNotificationAsync(service.Id, serviceDto.AssignedUserId.Value);
                }

                // **AUDIT LOG**
                var changeDescription = changes.Any() ? string.Join(", ", changes) : "Servis güncellendi";
                await _loggingService.LogAsync("UPDATE_SERVICE", "Service", service.Id, changeDescription, userId: updatedByUserId);

                var resultDto = await MapToServiceDtoAsync(service);

                _logger.LogInformation("Service updated: {ServiceId} by user {UserId}", service.Id, updatedByUserId);

                return ServiceResult<ServiceDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service: {ServiceId}", serviceDto.Id);
                await _loggingService.LogErrorAsync(ex, "UPDATE_SERVICE_ERROR", "Service", serviceDto.Id, userId: updatedByUserId);
                return ServiceResult<ServiceDto>.Failure("Servis güncelleme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Servis durumunu değiştirir
        /// State machine pattern ile workflow kontrolü
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeServiceStatusAsync(int serviceId, ServiceStatus newStatus, int changedByUserId, string? notes = null)
        {
            try
            {
                var service = await _serviceRepository.GetServiceWithDetailsAsync(serviceId);
                if (service == null)
                {
                    return ServiceResult<bool>.Failure("Servis bulunamadı.");
                }

                // **STATE TRANSITION VALIDATION**
                var validTransition = IsValidStatusTransition(service.Status, newStatus);
                if (!validTransition)
                {
                    return ServiceResult<bool>.Failure($"'{service.Status}' durumundan '{newStatus}' durumuna geçiş yapılamaz.");
                }

                // **BUSINESS RULES**
                if (newStatus == ServiceStatus.InProgress && !service.AssignedUserId.HasValue)
                {
                    return ServiceResult<bool>.Failure("Atanmamış servis 'Devam Ediyor' durumuna alınamaz.");
                }

                if (newStatus == ServiceStatus.Completed)
                {
                    // Tüm task'lar tamamlanmış mı kontrol et
                    var incompleteTasks = await _taskRepository.GetTasksByServiceAsync(serviceId);
                    if (incompleteTasks.Any(t => !t.IsCompleted))
                    {
                        return ServiceResult<bool>.Failure("Tüm görevler tamamlanmadan servis bitirilemez.");
                    }
                }

                var oldStatus = service.Status;
                service.Status = newStatus;
                service.UpdatedDate = DateTime.Now;

                // **STATUS-SPECIFIC UPDATES**
                switch (newStatus)
                {
                    case ServiceStatus.InProgress:
                        if (!service.ServiceStartDateTime.HasValue)
                            service.ServiceStartDateTime = DateTime.Now;
                        break;

                    case ServiceStatus.Completed:
                        service.ServiceEndDateTime = DateTime.Now;
                        break;

                    case ServiceStatus.Cancelled:
                        // İptal nedeni notes'a eklenir
                        service.TechnicianNotes = string.IsNullOrEmpty(service.TechnicianNotes)
                            ? $"İptal Nedeni: {notes}"
                            : $"{service.TechnicianNotes}\n\nİptal Nedeni: {notes}";
                        break;
                }

                await _serviceRepository.UpdateAsync(service);
                await _serviceRepository.SaveChangesAsync();

                // **NOTIFICATIONS**
                await SendStatusChangeNotificationsAsync(service, oldStatus, newStatus, changedByUserId);

                // **AUDIT LOG**
                await _loggingService.LogAsync("CHANGE_SERVICE_STATUS", "Service", serviceId,
                    $"Servis durumu değiştirildi: {oldStatus} → {newStatus}" +
                    (string.IsNullOrEmpty(notes) ? "" : $" (Not: {notes})"),
                    userId: changedByUserId);

                _logger.LogInformation("Service status changed: {ServiceId} from {OldStatus} to {NewStatus} by user {UserId}",
                    serviceId, oldStatus, newStatus, changedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing service status: {ServiceId}", serviceId);
                await _loggingService.LogErrorAsync(ex, "CHANGE_SERVICE_STATUS_ERROR", "Service", serviceId, userId: changedByUserId);
                return ServiceResult<bool>.Failure("Servis durumu değiştirme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Servise görev ekler
        /// </summary>
        public async Task<ServiceResult<ServiceTaskDto>> AddTaskToServiceAsync(int serviceId, ServiceTaskDto taskDto, int createdByUserId)
        {
            try
            {
                var service = await _serviceRepository.GetByIdAsync(serviceId);
                if (service == null)
                {
                    return ServiceResult<ServiceTaskDto>.Failure("Servis bulunamadı.");
                }

                if (service.Status == ServiceStatus.Completed)
                {
                    return ServiceResult<ServiceTaskDto>.Failure("Tamamlanmış servise görev eklenemez.");
                }

                var task = new ServiceTask
                {
                    ServiceId = serviceId,
                    Description = taskDto.Description.Trim(),
                    Priority = taskDto.Priority,
                    EstimatedMinutes = 60,
                    CreatedDate = DateTime.Now
                };

                await _taskRepository.AddAsync(task);
                await _taskRepository.SaveChangesAsync();

                // **AUDIT LOG**
                await _loggingService.LogAsync("ADD_SERVICE_TASK", "ServiceTask", task.Id,
                    $"Servise görev eklendi: {task.Description} (Servis: {service.Title})",
                    userId: createdByUserId);

                var resultDto = new ServiceTaskDto
                {
                    Id = task.Id,
                    Description = task.Description,
                    Priority = task.Priority,
                    IsCompleted = task.IsCompleted,
                    CompletedDate = task.CompletedDate
                };

                return ServiceResult<ServiceTaskDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding task to service: {ServiceId}", serviceId);
                return ServiceResult<ServiceTaskDto>.Failure("Görev ekleme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Görevi tamamlar
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteTaskAsync(int taskId, int completedByUserId, string? notes = null)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(taskId, t => t.Service);
                if (task == null)
                {
                    return ServiceResult<bool>.Failure("Görev bulunamadı.");
                }

                if (task.IsCompleted)
                {
                    return ServiceResult<bool>.Failure("Görev zaten tamamlanmış.");
                }

                var success = await _taskRepository.CompleteTaskAsync(taskId, completedByUserId);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("Görev tamamlama başarısız.");
                }

                await _taskRepository.SaveChangesAsync();

                // **UPDATE SERVICE PROGRESS**
                var progressPercentage = await _taskRepository.GetServiceProgressPercentageAsync(task.ServiceId);

                // **AUTO-COMPLETE SERVICE** if all tasks done
                if (progressPercentage >= 100)
                {
                    var allTasksCompleted = await CheckAllTasksCompletedAsync(task.ServiceId);
                    if (allTasksCompleted && task.Service?.Status == ServiceStatus.InProgress)
                    {
                        await ChangeServiceStatusAsync(task.ServiceId, ServiceStatus.Testing, completedByUserId, "Tüm görevler tamamlandı");
                    }
                }

                // **AUDIT LOG**
                await _loggingService.LogAsync("COMPLETE_SERVICE_TASK", "ServiceTask", taskId,
                    $"Görev tamamlandı: {task.Description}" + (string.IsNullOrEmpty(notes) ? "" : $" (Not: {notes})"),
                    userId: completedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task: {TaskId}", taskId);
                return ServiceResult<bool>.Failure("Görev tamamlama sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcının servislerini getirir
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ServiceListDto>>> GetMyServicesAsync(int userId)
        {
            try
            {
                var services = await _serviceRepository.GetServicesByUserAsync(userId);
                var serviceDtos = services.Select(MapToServiceListDto).ToList();

                return ServiceResult<IEnumerable<ServiceListDto>>.Success(serviceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user services: {UserId}", userId);
                return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Kullanıcı servisleri getirme sırasında hata oluştu.");
            }
        }

        /// <summary>
        /// Bekleyen servisleri getirir
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ServiceListDto>>> GetPendingServicesAsync()
        {
            try
            {
                var services = await _serviceRepository.GetPendingServicesAsync();
                var serviceDtos = services.Select(MapToServiceListDto).ToList();

                return ServiceResult<IEnumerable<ServiceListDto>>.Success(serviceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending services");
                return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Bekleyen servisler getirme sırasında hata oluştu.");
            }
        }

        /// <summary>
        /// Geciken servisleri getirir
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ServiceListDto>>> GetOverdueServicesAsync()
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
                return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Geciken servisler getirme sırasında hata oluştu.");
            }
        }

        /// <summary>
        /// Bugünkü servisleri getirir
        /// </summary>
        public async Task<ServiceResult<IEnumerable<ServiceListDto>>> GetTodayServicesAsync()
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
                return ServiceResult<IEnumerable<ServiceListDto>>.Failure("Bugünkü servisler getirme sırasında hata oluştu.");
            }
        }

        /// <summary>
        /// Genel servis istatistiklerini getirir
        /// </summary>
        public async Task<ServiceResult<ServiceStatsDto>> GetServiceStatsAsync()
        {
            try
            {
                var allServices = await _serviceRepository.GetAllAsync();
                var now = DateTime.Now;
                var monthStart = new DateTime(now.Year, now.Month, 1);

                var stats = new ServiceStatsDto
                {
                    TotalServices = allServices.Count(),
                    PendingServices = allServices.Count(s => s.Status == ServiceStatus.Pending),
                    InProgressServices = allServices.Count(s => s.Status == ServiceStatus.InProgress),
                    CompletedServices = allServices.Count(s => s.Status == ServiceStatus.Completed),
                    OverdueServices = allServices.Count(s => s.IsOverdue),
                    TodayServices = allServices.Count(s => s.ExpectedCompletionDate?.Date == DateTime.Today),
                    TotalRevenue = allServices.Where(s => s.ServiceAmount.HasValue).Sum(s => s.ServiceAmount!.Value),
                    MonthlyRevenue = allServices.Where(s => s.CreatedDate >= monthStart && s.ServiceAmount.HasValue)
                                              .Sum(s => s.ServiceAmount!.Value)
                };

                // **COMPLETION TIME CALCULATION**
                var completedServices = allServices.Where(s => s.Status == ServiceStatus.Completed &&
                                                             s.ServiceStartDateTime.HasValue &&
                                                             s.ServiceEndDateTime.HasValue);

                if (completedServices.Any())
                {
                    var totalDays = completedServices.Select(s => (s.ServiceEndDateTime!.Value - s.ServiceStartDateTime!.Value).TotalDays);
                    stats.AverageCompletionDays = totalDays.Average();
                }

                return ServiceResult<ServiceStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service stats");
                return ServiceResult<ServiceStatsDto>.Failure("Servis istatistikleri alınırken hata oluştu.");
            }
        }

        // **PRIVATE HELPER METHODS**

        /// <summary>
        /// Status transition'ının geçerli olup olmadığını kontrol eder
        /// </summary>
        private static bool IsValidStatusTransition(ServiceStatus currentStatus, ServiceStatus newStatus)
        {
            return currentStatus switch
            {
                ServiceStatus.Pending => newStatus is ServiceStatus.Accepted or ServiceStatus.Rejected or ServiceStatus.Cancelled,
                ServiceStatus.Accepted => newStatus is ServiceStatus.InProgress or ServiceStatus.Cancelled,
                ServiceStatus.InProgress => newStatus is ServiceStatus.WaitingForParts or ServiceStatus.Testing or ServiceStatus.Completed or ServiceStatus.Cancelled,
                ServiceStatus.WaitingForParts => newStatus is ServiceStatus.InProgress or ServiceStatus.Cancelled,
                ServiceStatus.Testing => newStatus is ServiceStatus.Completed or ServiceStatus.InProgress or ServiceStatus.Cancelled,
                ServiceStatus.Completed => false, // Completed services cannot be changed
                ServiceStatus.Cancelled => false, // Cancelled services cannot be changed
                ServiceStatus.Rejected => false, // Rejected services cannot be changed
                _ => false
            };
        }

        /// <summary>
        /// Kullanıcının servisi düzenleme yetkisi var mı kontrol eder
        /// </summary>
        private async Task<bool> CanUserEditServiceAsync(Service service, int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // Admin ve SuperAdmin her şeyi düzenleyebilir
            if (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
                return true;

            // Atanan teknisyen kendi servisini düzenleyebilir
            if (service.AssignedUserId == userId && user.Role == UserRole.Technician)
                return true;

            return false;
        }

        /// <summary>
        /// Müsait teknisyen bulur (auto-assignment için)
        /// </summary>
        private async Task<int?> FindAvailableTechnicianAsync()
        {
            try
            {
                var technicians = await _userRepository.GetByRoleAsync(UserRole.Technician);
                var activeTechnicians = technicians.Where(t => t.IsActive).ToList();

                if (!activeTechnicians.Any())
                    return null;

                // En az servisi olan teknisyeni bul
                var technicianWorkloads = new List<(User Technician, int ServiceCount)>();

                foreach (var technician in activeTechnicians)
                {
                    var activeServices = await _serviceRepository.GetServicesByUserAsync(technician.Id);
                    var activeCount = activeServices.Count(s => s.Status == ServiceStatus.InProgress || s.Status == ServiceStatus.Accepted);
                    technicianWorkloads.Add((technician, activeCount));
                }

                var availableTechnician = technicianWorkloads.OrderBy(tw => tw.ServiceCount).First();
                return availableTechnician.Technician.Id;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding available technician");
                return null;
            }
        }

        /// <summary>
        /// Önceliğe göre beklenen tamamlanma tarihi hesaplar
        /// </summary>
        private static DateTime CalculateExpectedDate(Priority priority)
        {
            var businessDays = priority switch
            {
                Priority.Critical => 1,
                Priority.High => 3,
                Priority.Normal => 7,
                Priority.Low => 14,
                _ => 7
            };

            var expectedDate = DateTime.Now;
            var addedDays = 0;

            while (addedDays < businessDays)
            {
                expectedDate = expectedDate.AddDays(1);

                // Skip weekends
                if (expectedDate.DayOfWeek != DayOfWeek.Saturday &&
                    expectedDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    addedDays++;
                }
            }

            return expectedDate;
        }

        /// <summary>
        /// Servisin tüm görevleri tamamlanmış mı kontrol eder
        /// </summary>
        private async Task<bool> CheckAllTasksCompletedAsync(int serviceId)
        {
            var tasks = await _taskRepository.GetTasksByServiceAsync(serviceId);
            return tasks.All(t => t.IsCompleted);
        }

        /// <summary>
        /// Servis oluşturulma bildirimleri gönderir
        /// </summary>
        private async Task SendServiceCreatedNotificationsAsync(Service service, int createdByUserId)
        {
            try
            {
                // Atanan teknisyene bildirim gönder
                if (service.AssignedUserId.HasValue)
                {
                    await _notificationService.SendServiceAssignedNotificationAsync(service.Id, service.AssignedUserId.Value);
                }

                // Yöneticilere bildirim gönder
                var managers = await _userRepository.GetByRoleAsync(UserRole.Admin);
                foreach (var manager in managers.Where(m => m.IsActive && m.Id != createdByUserId))
                {
                    await _notificationService.SendNewServiceNotificationAsync(service.Id, manager.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error sending service creation notifications");
            }
        }

        /// <summary>
        /// Status değişikliği bildirimleri gönderir
        /// </summary>
        private async Task SendStatusChangeNotificationsAsync(Service service, ServiceStatus oldStatus, ServiceStatus newStatus, int changedByUserId)
        {
            try
            {
                // Atanan teknisyene bildirim
                if (service.AssignedUserId.HasValue && service.AssignedUserId != changedByUserId)
                {
                    await _notificationService.SendServiceStatusChangedNotificationAsync(service.Id, service.AssignedUserId.Value, oldStatus, newStatus);
                }

                // Müşteriye kritik durum değişikliklerinde bildirim
                if (newStatus == ServiceStatus.Completed || newStatus == ServiceStatus.Cancelled)
                {
                    await _notificationService.SendCustomerServiceNotificationAsync(service.Id, service.CustomerId, newStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error sending status change notifications");
            }
        }

        /// <summary>
        /// Service entity'sini DTO'ya dönüştürür
        /// </summary>
        private async Task<ServiceDto> MapToServiceDtoAsync(Service service)
        {
            var dto = new ServiceDto
            {
                Id = service.Id,
                Title = service.Title,
                Description = service.Description,
                CustomerId = service.CustomerId,
                AssignedUserId = service.AssignedUserId,
                Status = service.Status,
                Priority = service.Priority,
                ServiceStartDateTime = service.ServiceStartDateTime,
                ServiceEndDateTime = service.ServiceEndDateTime,
                ExpectedCompletionDate = service.ExpectedCompletionDate,
                ServiceAmount = service.ServiceAmount,
                IsWarrantyService = service.IsWarrantyService,
                DeviceModel = service.DeviceModel,
                DeviceSerialNumber = service.DeviceSerialNumber,
                ProblemDescription = service.ProblemDescription,
                SolutionDescription = service.SolutionDescription,
                UsedParts = service.UsedParts,
                TechnicianNotes = service.TechnicianNotes,
                CustomerNotes = service.CustomerNotes,
                CreatedDate = service.CreatedDate,
                UpdatedDate = service.UpdatedDate
            };

            // Navigation properties
            if (service.Customer != null)
            {
                dto.Customer = new CustomerDto
                {
                    Id = service.Customer.Id,
                    CompanyName = service.Customer.CompanyName,
                    ContactPerson = service.Customer.ContactPerson,
                    Email = service.Customer.Email,
                    PhoneNumber = service.Customer.PhoneNumber
                };
            }

            if (service.AssignedUser != null)
            {
                dto.AssignedUser = new UserDto
                {
                    Id = service.AssignedUser.Id,
                    Username = service.AssignedUser.Username,
                    FirstName = service.AssignedUser.FirstName,
                    LastName = service.AssignedUser.LastName
                };
            }

            // Tasks
            if (service.ServiceTasks?.Any() == true)
            {
                dto.Tasks = service.ServiceTasks.Select(t => new ServiceTaskDto
                {
                    Id = t.Id,
                    Description = t.Description,
                    Priority = t.Priority,
                    IsCompleted = t.IsCompleted,
                    CompletedDate = t.CompletedDate,
                    CompletedByUserName = t.CompletedByUser?.FullName
                }).ToList();
            }

            return dto;
        }

        /// <summary>
        /// Service entity'sini ServiceListDto'ya dönüştürür
        /// </summary>
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

        /// <summary>
        /// Servis verilerini doğrular
        /// </summary>
        private async Task<ServiceResult<bool>> ValidateServiceDataAsync(ServiceDto serviceDto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(serviceDto.Title))
                errors.Add("Servis başlığı gereklidir.");

            if (string.IsNullOrWhiteSpace(serviceDto.Description))
                errors.Add("Servis açıklaması gereklidir.");

            if (serviceDto.CustomerId <= 0)
                errors.Add("Geçerli bir müşteri seçiniz.");

            if (serviceDto.ServiceAmount.HasValue && serviceDto.ServiceAmount.Value < 0)
                errors.Add("Servis tutarı negatif olamaz.");

            // Check customer exists
            var customer = await _customerRepository.GetByIdAsync(serviceDto.CustomerId);
            if (customer == null)
                errors.Add("Seçilen müşteri bulunamadı.");

            // Check assigned user exists and is technician
            if (serviceDto.AssignedUserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(serviceDto.AssignedUserId.Value);
                if (user == null)
                    errors.Add("Seçilen kullanıcı bulunamadı.");
                else if (user.Role != UserRole.Technician && user.Role != UserRole.Admin && user.Role != UserRole.SuperAdmin)
                    errors.Add("Sadece teknisyen veya yönetici rolündeki kullanıcılar servise atanabilir.");
            }

            if (errors.Any())
            {
                return ServiceResult<bool>.ValidationFailure(errors);
            }

            return ServiceResult<bool>.Success(true);
        }

        // **NOT IMPLEMENTED METHODS** - Implement based on specific needs
        public Task<ServiceResult<bool>> DeleteServiceAsync(int serviceId, int deletedByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<ServiceDto>> GetServiceByIdAsync(int serviceId) => throw new NotImplementedException();
        public Task<ServiceResult<PagedResultDto<ServiceListDto>>> GetServicesPagedAsync(int pageNumber, int pageSize, ServiceStatus? status = null, int? customerId = null, int? assignedUserId = null, DateTime? startDate = null, DateTime? endDate = null) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> AssignServiceAsync(int serviceId, int assignedUserId, int assignedByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> UnassignServiceAsync(int serviceId, int unassignedByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> CompleteServiceAsync(int serviceId, string solutionDescription, int completedByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> CancelServiceAsync(int serviceId, string reason, int cancelledByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> UpdateTaskAsync(ServiceTaskDto taskDto, int updatedByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> DeleteTaskAsync(int taskId, int deletedByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<IEnumerable<ServiceTaskDto>>> GetServiceTasksAsync(int serviceId) => throw new NotImplementedException();
        public Task<ServiceResult<ServiceStatsDto>> GetUserServiceStatsAsync(int userId) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> SetServicePriorityAsync(int serviceId, Priority priority, int changedByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> UpdateServiceAmountAsync(int serviceId, decimal amount, int updatedByUserId) => throw new NotImplementedException();
        public Task<ServiceResult<bool>> AddServiceNotesAsync(int serviceId, string notes, int addedByUserId, bool isCustomerNote = false) => throw new NotImplementedException();
        public Task<ServiceResult<double>> CalculateServiceProgressAsync(int serviceId) => throw new NotImplementedException();
    }
}
