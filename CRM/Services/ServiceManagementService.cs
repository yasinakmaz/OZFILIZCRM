namespace CRM.Services
{
    /// <summary>
    /// Servis yönetimi için ana business logic servisi
    /// Servis yaşam döngüsü, görev yönetimi ve iş akışı kontrolü
    /// </summary>
    public class ServiceManagementService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUserRepository _userRepository;
        private readonly TeknikServisDbContext _context;
        private readonly LoggingService _loggingService;

        public ServiceManagementService(
            IServiceRepository serviceRepository,
            ICustomerRepository customerRepository,
            IUserRepository userRepository,
            TeknikServisDbContext context,
            LoggingService loggingService)
        {
            _serviceRepository = serviceRepository;
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _context = context;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Sayfalama ile servis listesini getirir
        /// Role-based filtering ve search desteği
        /// </summary>
        public async Task<(IEnumerable<Service> services, int totalCount)> GetServicesPagedAsync(
            int pageNumber,
            int pageSize,
            ServiceStatus? status = null,
            int? customerId = null,
            int? assignedUserId = null,
            string? searchTerm = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                return await _serviceRepository.GetServicesPagedAsync(
                    pageNumber, pageSize, status, customerId, searchTerm);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GET_SERVICES_PAGED", "Service");
                throw;
            }
        }

        /// <summary>
        /// Servis detaylarını getirir
        /// Tüm ilişkili veriler ile birlikte (müşteri, görevler, atanmış kullanıcılar)
        /// </summary>
        public async Task<Service?> GetServiceWithDetailsAsync(int serviceId)
        {
            try
            {
                return await _serviceRepository.GetServiceWithDetailsAsync(serviceId);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GET_SERVICE_DETAILS", "Service", serviceId);
                throw;
            }
        }

        /// <summary>
        /// Yeni servis oluşturur
        /// Validation ve business rules kontrolü yapar
        /// </summary>
        public async Task<ServiceResult<Service>> CreateServiceAsync(ServiceDto serviceDto, int createdByUserId)
        {
            try
            {
                // Validation kontrolü
                var validationResult = await ValidateServiceAsync(serviceDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<Service>.Failure(validationResult.ErrorMessage!);
                }

                // Müşteri varlık kontrolü
                var customer = await _customerRepository.GetByIdAsync(serviceDto.CustomerId);
                if (customer == null || !customer.IsActive)
                {
                    return ServiceResult<Service>.Failure("Geçerli bir müşteri seçiniz.");
                }

                // Transaction başlat
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Servis entity'si oluştur
                    var service = new Service
                    {
                        CustomerId = serviceDto.CustomerId,
                        CreatedByUserId = createdByUserId,
                        Status = ServiceStatus.Pending,
                        ServiceAmount = serviceDto.ServiceAmount,
                        ScheduledDateTime = serviceDto.ScheduledDateTime,
                        Notes = serviceDto.Notes?.Trim(),
                        CreatedDate = DateTime.Now
                    };

                    var createdService = await _serviceRepository.AddAsync(service);

                    // Görevleri oluştur
                    if (serviceDto.Tasks != null && serviceDto.Tasks.Any())
                    {
                        foreach (var taskDto in serviceDto.Tasks)
                        {
                            var serviceTask = new ServiceTask
                            {
                                ServiceId = createdService.Id,
                                Description = taskDto.Description.Trim(),
                                Priority = taskDto.Priority,
                                IsCompleted = false,
                                CreatedDate = DateTime.Now
                            };

                            await _context.ServiceTasks.AddAsync(serviceTask);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // İşlemi logla
                    await _loggingService.LogAsync(
                        "CREATE_SERVICE",
                        "Service",
                        createdService.Id,
                        $"Yeni servis oluşturuldu - Müşteri: {customer.CompanyName}",
                        userId: createdByUserId,
                        newValues: new
                        {
                            ServiceId = createdService.Id,
                            Customer = customer.CompanyName,
                            ScheduledDate = createdService.ScheduledDateTime,
                            TaskCount = serviceDto.Tasks?.Count() ?? 0
                        });

                    return ServiceResult<Service>.Success(createdService);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "CREATE_SERVICE", "Service", userId: createdByUserId);
                return ServiceResult<Service>.Failure("Servis oluşturulurken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Servis bilgilerini günceller
        /// Status değişikliği ayrı metodla yapılır
        /// </summary>
        public async Task<ServiceResult<Service>> UpdateServiceAsync(int serviceId, ServiceDto serviceDto, int userId)
        {
            try
            {
                var existingService = await _serviceRepository.GetServiceWithDetailsAsync(serviceId);
                if (existingService == null)
                {
                    return ServiceResult<Service>.Failure("Servis bulunamadı.");
                }

                // Authorization kontrolü - sadece atanmış kullanıcılar veya yöneticiler
                if (!await CanUserModifyServiceAsync(serviceId, userId))
                {
                    return ServiceResult<Service>.Failure("Bu servisi düzenleme yetkiniz bulunmuyor.");
                }

                // Validation kontrolü
                var validationResult = await ValidateServiceAsync(serviceDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<Service>.Failure(validationResult.ErrorMessage!);
                }

                // Eski değerleri sakla
                var oldValues = new
                {
                    existingService.ServiceAmount,
                    existingService.ScheduledDateTime,
                    existingService.Notes
                };

                // Güncelleme işlemi
                existingService.ServiceAmount = serviceDto.ServiceAmount;
                existingService.ScheduledDateTime = serviceDto.ScheduledDateTime;
                existingService.Notes = serviceDto.Notes?.Trim();
                existingService.UpdatedDate = DateTime.Now;

                var updatedService = await _serviceRepository.UpdateAsync(existingService);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "UPDATE_SERVICE",
                    "Service",
                    serviceId,
                    $"Servis güncellendi - #{serviceId}",
                    userId: userId,
                    oldValues: oldValues,
                    newValues: new
                    {
                        updatedService.ServiceAmount,
                        updatedService.ScheduledDateTime,
                        updatedService.Notes
                    });

                return ServiceResult<Service>.Success(updatedService);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "UPDATE_SERVICE", "Service", serviceId, userId: userId);
                return ServiceResult<Service>.Failure("Servis güncellenirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Servis durumunu değiştirir
        /// İş akışı kurallarını kontrol eder
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeServiceStatusAsync(
            int serviceId,
            ServiceStatus newStatus,
            int userId,
            string? reason = null)
        {
            try
            {
                var service = await _serviceRepository.GetServiceWithDetailsAsync(serviceId);
                if (service == null)
                {
                    return ServiceResult<bool>.Failure("Servis bulunamadı.");
                }

                // Status geçiş kontrolü
                var canTransition = await CanTransitionToStatusAsync(service.Status, newStatus, userId);
                if (!canTransition.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(canTransition.ErrorMessage!);
                }

                var oldStatus = service.Status;
                service.Status = newStatus;
                service.UpdatedDate = DateTime.Now;

                // Status'a özel işlemler
                switch (newStatus)
                {
                    case ServiceStatus.InProgress:
                        service.ServiceStartDateTime = DateTime.Now;
                        break;
                    case ServiceStatus.WaitingApproval:
                        service.ServiceEndDateTime = DateTime.Now;
                        break;
                    case ServiceStatus.Completed:
                        service.ApprovedDate = DateTime.Now;
                        service.ApprovedByUserId = userId;
                        if (!service.ServiceEndDateTime.HasValue)
                            service.ServiceEndDateTime = DateTime.Now;
                        break;
                }

                await _serviceRepository.UpdateAsync(service);

                // Status değişikliğini logla
                await _loggingService.LogServiceStatusChangeAsync(
                    serviceId, oldStatus.ToString(), newStatus.ToString(), userId, reason);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "CHANGE_SERVICE_STATUS", "Service", serviceId, userId: userId);
                return ServiceResult<bool>.Failure("Servis durumu değiştirilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Servise kullanıcı atar
        /// Sadece yöneticiler ve süpervizörler kullanabilir
        /// </summary>
        public async Task<ServiceResult<bool>> AssignUserToServiceAsync(
            int serviceId,
            int userId,
            int assignedByUserId)
        {
            try
            {
                var service = await _serviceRepository.GetByIdAsync(serviceId);
                if (service == null)
                {
                    return ServiceResult<bool>.Failure("Servis bulunamadı.");
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return ServiceResult<bool>.Failure("Geçerli bir kullanıcı seçiniz.");
                }

                // Mevcut atama var mı kontrol et
                var existingAssignment = await _context.ServiceUsers
                    .FirstOrDefaultAsync(su => su.ServiceId == serviceId && su.UserId == userId && su.IsActive);

                if (existingAssignment != null)
                {
                    return ServiceResult<bool>.Failure("Bu kullanıcı zaten servise atanmış.");
                }

                // Yeni atama oluştur
                var serviceUser = new ServiceUser
                {
                    ServiceId = serviceId,
                    UserId = userId,
                    AssignedByUserId = assignedByUserId,
                    AssignedDate = DateTime.Now,
                    IsActive = true
                };

                await _context.ServiceUsers.AddAsync(serviceUser);
                await _context.SaveChangesAsync();

                // İşlemi logla
                await _loggingService.LogAsync(
                    "ASSIGN_USER_TO_SERVICE",
                    "ServiceUser",
                    serviceId,
                    $"Kullanıcı servise atandı - Servis #{serviceId}, Kullanıcı: {user.Username}",
                    userId: assignedByUserId,
                    newValues: new { ServiceId = serviceId, UserId = userId, UserName = user.Username });

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "ASSIGN_USER_TO_SERVICE", "Service", serviceId, userId: assignedByUserId);
                return ServiceResult<bool>.Failure("Kullanıcı atanırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Servisten kullanıcı atamasını kaldırır
        /// </summary>
        public async Task<ServiceResult<bool>> RemoveUserFromServiceAsync(
            int serviceId,
            int userId,
            int removedByUserId)
        {
            try
            {
                var serviceUser = await _context.ServiceUsers
                    .FirstOrDefaultAsync(su => su.ServiceId == serviceId && su.UserId == userId && su.IsActive);

                if (serviceUser == null)
                {
                    return ServiceResult<bool>.Failure("Kullanıcı bu servise atanmamış.");
                }

                var user = await _userRepository.GetByIdAsync(userId);

                // Soft delete
                serviceUser.IsActive = false;
                serviceUser.RemovedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // İşlemi logla
                await _loggingService.LogAsync(
                    "REMOVE_USER_FROM_SERVICE",
                    "ServiceUser",
                    serviceId,
                    $"Kullanıcı servisten çıkarıldı - Servis #{serviceId}, Kullanıcı: {user?.Username}",
                    userId: removedByUserId,
                    oldValues: new { ServiceId = serviceId, UserId = userId, UserName = user?.Username });

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "REMOVE_USER_FROM_SERVICE", "Service", serviceId, userId: removedByUserId);
                return ServiceResult<bool>.Failure("Kullanıcı çıkarılırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Servise görev ekler
        /// </summary>
        public async Task<ServiceResult<ServiceTask>> AddTaskToServiceAsync(
            int serviceId,
            ServiceTaskDto taskDto,
            int userId)
        {
            try
            {
                var service = await _serviceRepository.GetByIdAsync(serviceId);
                if (service == null)
                {
                    return ServiceResult<ServiceTask>.Failure("Servis bulunamadı.");
                }

                // Authorization kontrolü
                if (!await CanUserModifyServiceAsync(serviceId, userId))
                {
                    return ServiceResult<ServiceTask>.Failure("Bu servise görev ekleme yetkiniz bulunmuyor.");
                }

                var serviceTask = new ServiceTask
                {
                    ServiceId = serviceId,
                    Description = taskDto.Description.Trim(),
                    Priority = taskDto.Priority,
                    IsCompleted = false,
                    CreatedDate = DateTime.Now
                };

                await _context.ServiceTasks.AddAsync(serviceTask);
                await _context.SaveChangesAsync();

                // İşlemi logla
                await _loggingService.LogAsync(
                    "ADD_TASK_TO_SERVICE",
                    "ServiceTask",
                    serviceTask.Id,
                    $"Servise görev eklendi - Servis #{serviceId}",
                    userId: userId,
                    newValues: serviceTask);

                return ServiceResult<ServiceTask>.Success(serviceTask);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "ADD_TASK_TO_SERVICE", "Service", serviceId, userId: userId);
                return ServiceResult<ServiceTask>.Failure("Görev eklenirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Görevi tamamlanmış olarak işaretler
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteTaskAsync(int taskId, int completedByUserId)
        {
            try
            {
                var task = await _context.ServiceTasks
                    .Include(st => st.Service)
                    .FirstOrDefaultAsync(st => st.Id == taskId);

                if (task == null)
                {
                    return ServiceResult<bool>.Failure("Görev bulunamadı.");
                }

                // Authorization kontrolü
                if (!await CanUserModifyServiceAsync(task.ServiceId, completedByUserId))
                {
                    return ServiceResult<bool>.Failure("Bu görevi tamamlama yetkiniz bulunmuyor.");
                }

                if (task.IsCompleted)
                {
                    return ServiceResult<bool>.Failure("Bu görev zaten tamamlanmış.");
                }

                task.IsCompleted = true;
                task.CompletedDate = DateTime.Now;
                task.CompletedByUserId = completedByUserId;

                await _context.SaveChangesAsync();

                // İşlemi logla
                await _loggingService.LogAsync(
                    "COMPLETE_TASK",
                    "ServiceTask",
                    taskId,
                    $"Görev tamamlandı - Servis #{task.ServiceId}",
                    userId: completedByUserId,
                    newValues: new { task.IsCompleted, task.CompletedDate });

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "COMPLETE_TASK", "ServiceTask", taskId, userId: completedByUserId);
                return ServiceResult<bool>.Failure("Görev tamamlanırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kullanıcının servisi değiştirme yetkisi var mı kontrol eder
        /// </summary>
        private async Task<bool> CanUserModifyServiceAsync(int serviceId, int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // Admin ve Supervisor her servisi değiştirebilir
            if (user.Role == UserRole.Admin || user.Role == UserRole.Supervisor)
                return true;

            // Teknisyen sadece atandığı servisleri değiştirebilir
            if (user.Role == UserRole.Technician)
            {
                return await _context.ServiceUsers
                    .AnyAsync(su => su.ServiceId == serviceId && su.UserId == userId && su.IsActive);
            }

            return false;
        }

        /// <summary>
        /// Status geçişinin geçerli olup olmadığını kontrol eder
        /// </summary>
        private async Task<ServiceResult<bool>> CanTransitionToStatusAsync(
            ServiceStatus currentStatus,
            ServiceStatus newStatus,
            int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ServiceResult<bool>.Failure("Kullanıcı bulunamadı.");

            // Aynı status'a geçiş yapılamaz
            if (currentStatus == newStatus)
                return ServiceResult<bool>.Failure("Servis zaten bu durumda.");

            // Geçerli status geçişleri
            var validTransitions = new Dictionary<ServiceStatus, List<ServiceStatus>>
            {
                [ServiceStatus.Pending] = new() { ServiceStatus.InProgress, ServiceStatus.Cancelled },
                [ServiceStatus.InProgress] = new() { ServiceStatus.WaitingApproval, ServiceStatus.Cancelled },
                [ServiceStatus.WaitingApproval] = new() { ServiceStatus.Completed, ServiceStatus.InProgress },
                [ServiceStatus.Completed] = new() { }, // Tamamlanan servis başka duruma geçemez
                [ServiceStatus.Cancelled] = new() { ServiceStatus.Pending } // İptal edilen servis tekrar başlatılabilir
            };

            if (!validTransitions[currentStatus].Contains(newStatus))
                return ServiceResult<bool>.Failure("Geçersiz durum değişikliği.");

            // Yetki kontrolü
            switch (newStatus)
            {
                case ServiceStatus.Completed:
                    // Sadece admin tamamlayabilir
                    if (user.Role != UserRole.Admin)
                        return ServiceResult<bool>.Failure("Servisi tamamlama yetkiniz bulunmuyor.");
                    break;

                case ServiceStatus.Cancelled:
                    // Admin ve Supervisor iptal edebilir
                    if (user.Role != UserRole.Admin && user.Role != UserRole.Supervisor)
                        return ServiceResult<bool>.Failure("Servisi iptal etme yetkiniz bulunmuyor.");
                    break;
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// Servis validation işlemi
        /// </summary>
        private async Task<ServiceResult<bool>> ValidateServiceAsync(ServiceDto serviceDto)
        {
            if (serviceDto.CustomerId <= 0)
                return ServiceResult<bool>.Failure("Geçerli bir müşteri seçiniz.");

            if (serviceDto.ScheduledDateTime <= DateTime.Now)
                return ServiceResult<bool>.Failure("Planlanan tarih gelecekte olmalıdır.");

            if (serviceDto.ServiceAmount.HasValue && serviceDto.ServiceAmount <= 0)
                return ServiceResult<bool>.Failure("Servis tutarı pozitif bir değer olmalıdır.");

            return ServiceResult<bool>.Success(true);
        }
    }
}
