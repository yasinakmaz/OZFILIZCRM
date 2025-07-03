namespace CRM.Services
{
    /// <summary>
    /// Müşteri servisi implementation
    /// Comprehensive customer management
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ILoggingService _loggingService;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerRepository customerRepository,
            IServiceRepository serviceRepository,
            ILoggingService loggingService,
            ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _serviceRepository = serviceRepository ?? throw new ArgumentNullException(nameof(serviceRepository));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Yeni müşteri oluşturur
        /// </summary>
        public async Task<ServiceResult<CustomerDto>> CreateCustomerAsync(CustomerDto customerDto, int createdByUserId)
        {
            try
            {
                // **VALIDATION**
                var validationResult = await ValidateCustomerDataAsync(customerDto);
                if (!validationResult.IsSuccess)
                {
                    return validationResult;
                }

                // **BUSINESS RULES**
                if (!string.IsNullOrEmpty(customerDto.TaxNumber))
                {
                    var existingCustomer = await _customerRepository.GetByTaxNumberAsync(customerDto.TaxNumber);
                    if (existingCustomer != null)
                    {
                        return ServiceResult<CustomerDto>.Failure("Bu vergi numarası zaten kayıtlı.");
                    }
                }

                // **CREATE ENTITY**
                var customer = new Customer
                {
                    CompanyName = customerDto.CompanyName.Trim(),
                    TaxNumber = customerDto.TaxNumber?.Trim(),
                    ContactPerson = customerDto.ContactPerson?.Trim(),
                    Email = customerDto.Email?.Trim().ToLower(),
                    PhoneNumber = customerDto.PhoneNumber?.Trim(),
                    MobileNumber = customerDto.MobileNumber?.Trim(),
                    Address = customerDto.Address?.Trim(),
                    City = customerDto.City?.Trim(),
                    District = customerDto.District?.Trim(),
                    PostalCode = customerDto.PostalCode?.Trim(),
                    CustomerType = customerDto.CustomerType,
                    IsActive = customerDto.IsActive,
                    Notes = customerDto.Notes?.Trim(),
                    CreditLimit = customerDto.CreditLimit,
                    PaymentTermDays = customerDto.PaymentTermDays,
                    CreatedDate = DateTime.Now
                };

                await _customerRepository.AddAsync(customer);
                await _customerRepository.SaveChangesAsync();

                // **AUDIT LOG**
                await _loggingService.LogAsync("CREATE_CUSTOMER", "Customer", customer.Id,
                    $"Yeni müşteri oluşturuldu: {customer.CompanyName}", userId: createdByUserId);

                // **RETURN DTO**
                var resultDto = MapToDto(customer);

                _logger.LogInformation("Customer created: {CustomerName} by user {UserId}",
                    customer.CompanyName, createdByUserId);

                return ServiceResult<CustomerDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                await _loggingService.LogErrorAsync(ex, "CREATE_CUSTOMER_ERROR", "Customer", userId: createdByUserId);
                return ServiceResult<CustomerDto>.Failure("Müşteri oluşturma sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteri bilgilerini günceller
        /// </summary>
        public async Task<ServiceResult<CustomerDto>> UpdateCustomerAsync(CustomerDto customerDto, int updatedByUserId)
        {
            try
            {
                // **VALIDATION**
                var validationResult = await ValidateCustomerDataAsync(customerDto);
                if (!validationResult.IsSuccess)
                {
                    return validationResult;
                }

                var customer = await _customerRepository.GetByIdAsync(customerDto.Id);
                if (customer == null)
                {
                    return ServiceResult<CustomerDto>.Failure("Müşteri bulunamadı.");
                }

                // **BUSINESS RULES**
                if (!string.IsNullOrEmpty(customerDto.TaxNumber) && customer.TaxNumber != customerDto.TaxNumber)
                {
                    var existingCustomer = await _customerRepository.GetByTaxNumberAsync(customerDto.TaxNumber);
                    if (existingCustomer != null && existingCustomer.Id != customer.Id)
                    {
                        return ServiceResult<CustomerDto>.Failure("Bu vergi numarası başka bir müşteri tarafından kullanılıyor.");
                    }
                }

                // **UPDATE ENTITY**
                customer.CompanyName = customerDto.CompanyName.Trim();
                customer.TaxNumber = customerDto.TaxNumber?.Trim();
                customer.ContactPerson = customerDto.ContactPerson?.Trim();
                customer.Email = customerDto.Email?.Trim().ToLower();
                customer.PhoneNumber = customerDto.PhoneNumber?.Trim();
                customer.MobileNumber = customerDto.MobileNumber?.Trim();
                customer.Address = customerDto.Address?.Trim();
                customer.City = customerDto.City?.Trim();
                customer.District = customerDto.District?.Trim();
                customer.PostalCode = customerDto.PostalCode?.Trim();
                customer.CustomerType = customerDto.CustomerType;
                customer.IsActive = customerDto.IsActive;
                customer.Notes = customerDto.Notes?.Trim();
                customer.CreditLimit = customerDto.CreditLimit;
                customer.PaymentTermDays = customerDto.PaymentTermDays;
                customer.UpdatedDate = DateTime.Now;

                await _customerRepository.UpdateAsync(customer);
                await _customerRepository.SaveChangesAsync();

                // **AUDIT LOG**
                await _loggingService.LogAsync("UPDATE_CUSTOMER", "Customer", customer.Id,
                    $"Müşteri güncellendi: {customer.CompanyName}", userId: updatedByUserId);

                var resultDto = MapToDto(customer);

                _logger.LogInformation("Customer updated: {CustomerName} by user {UserId}",
                    customer.CompanyName, updatedByUserId);

                return ServiceResult<CustomerDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {CustomerId}", customerDto.Id);
                await _loggingService.LogErrorAsync(ex, "UPDATE_CUSTOMER_ERROR", "Customer", customerDto.Id, userId: updatedByUserId);
                return ServiceResult<CustomerDto>.Failure("Müşteri güncelleme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteriyi siler (soft delete)
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteCustomerAsync(int customerId, int deletedByUserId)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return ServiceResult<bool>.Failure("Müşteri bulunamadı.");
                }

                // **BUSINESS RULES**
                var hasActiveServices = await _customerRepository.HasActiveServicesAsync(customerId);
                if (hasActiveServices)
                {
                    return ServiceResult<bool>.Failure("Aktif servisleri olan müşteri silinemez.");
                }

                // **SOFT DELETE**
                await _customerRepository.DeleteAsync(customer);
                await _customerRepository.SaveChangesAsync();

                // **AUDIT LOG**
                await _loggingService.LogAsync("DELETE_CUSTOMER", "Customer", customerId,
                    $"Müşteri silindi: {customer.CompanyName}", userId: deletedByUserId);

                _logger.LogInformation("Customer deleted: {CustomerName} by user {UserId}",
                    customer.CompanyName, deletedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {CustomerId}", customerId);
                await _loggingService.LogErrorAsync(ex, "DELETE_CUSTOMER_ERROR", "Customer", customerId, userId: deletedByUserId);
                return ServiceResult<bool>.Failure("Müşteri silme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// ID'ye göre müşteri getirir
        /// </summary>
        public async Task<ServiceResult<CustomerDto>> GetCustomerByIdAsync(int customerId)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return ServiceResult<CustomerDto>.Failure("Müşteri bulunamadı.");
                }

                var customerDto = MapToDto(customer);

                // **ADDITIONAL DATA**
                customerDto.TotalServiceAmount = await _customerRepository.GetTotalServiceAmountAsync(customerId);

                return ServiceResult<CustomerDto>.Success(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer: {CustomerId}", customerId);
                return ServiceResult<CustomerDto>.Failure("Müşteri getirme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşterileri sayfalı şekilde getirir
        /// </summary>
        public async Task<ServiceResult<PagedResultDto<CustomerDto>>> GetCustomersPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            try
            {
                var result = await _customerRepository.GetCustomersPagedAsync(pageNumber, pageSize, searchTerm);

                var customerDtos = result.Customers.Select(MapToDto).ToList();

                var pagedResult = new PagedResultDto<CustomerDto>
                {
                    Items = customerDtos,
                    TotalCount = result.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResultDto<CustomerDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers paged");
                return ServiceResult<PagedResultDto<CustomerDto>>.Failure("Müşteri listesi getirme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteri arama yapar
        /// </summary>
        public async Task<ServiceResult<IEnumerable<CustomerSearchDto>>> SearchCustomersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                {
                    return ServiceResult<IEnumerable<CustomerSearchDto>>.Success(Enumerable.Empty<CustomerSearchDto>());
                }

                var customers = await _customerRepository.SearchCustomersAsync(searchTerm);

                var searchDtos = customers.Select(c => new CustomerSearchDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    ContactPerson = c.ContactPerson,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    CustomerType = c.CustomerType
                }).ToList();

                return ServiceResult<IEnumerable<CustomerSearchDto>>.Success(searchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers with term: {SearchTerm}", searchTerm);
                return ServiceResult<IEnumerable<CustomerSearchDto>>.Failure("Müşteri arama sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteri istatistiklerini getirir
        /// </summary>
        public async Task<ServiceResult<CustomerStatsDto>> GetCustomerStatsAsync()
        {
            try
            {
                var allCustomers = await _customerRepository.GetAllAsync();

                var stats = new CustomerStatsDto
                {
                    TotalCustomers = allCustomers.Count(),
                    ActiveCustomers = allCustomers.Count(c => c.IsActive),
                    InactiveCustomers = allCustomers.Count(c => !c.IsActive),
                    CorporateCustomers = allCustomers.Count(c => c.CustomerType == CustomerType.Corporate),
                    IndividualCustomers = allCustomers.Count(c => c.CustomerType == CustomerType.Individual),
                    DealerCustomers = allCustomers.Count(c => c.CustomerType == CustomerType.Dealer)
                };

                // **CALCULATE REVENUE STATS**
                foreach (var customer in allCustomers)
                {
                    var totalAmount = await _customerRepository.GetTotalServiceAmountAsync(customer.Id);
                    stats.TotalServiceRevenue += totalAmount;
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
        /// Müşteriyi aktif hale getirir
        /// </summary>
        public async Task<ServiceResult<bool>> ActivateCustomerAsync(int customerId, int activatedByUserId)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return ServiceResult<bool>.Failure("Müşteri bulunamadı.");
                }

                customer.IsActive = true;
                customer.UpdatedDate = DateTime.Now;

                await _customerRepository.UpdateAsync(customer);
                await _customerRepository.SaveChangesAsync();

                await _loggingService.LogAsync("ACTIVATE_CUSTOMER", "Customer", customerId,
                    $"Müşteri aktif hale getirildi: {customer.CompanyName}", userId: activatedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating customer: {CustomerId}", customerId);
                return ServiceResult<bool>.Failure("Müşteri aktifleştirme sırasında hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteriyi pasif hale getirir
        /// </summary>
        public async Task<ServiceResult<bool>> DeactivateCustomerAsync(int customerId, int deactivatedByUserId)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return ServiceResult<bool>.Failure("Müşteri bulunamadı.");
                }

                // **BUSINESS RULES**
                var hasActiveServices = await _customerRepository.HasActiveServicesAsync(customerId);
                if (hasActiveServices)
                {
                    return ServiceResult<bool>.Failure("Aktif servisleri olan müşteri pasif hale getirilemez.");
                }

                customer.IsActive = false;
                customer.UpdatedDate = DateTime.Now;

                await _customerRepository.UpdateAsync(customer);
                await _customerRepository.SaveChangesAsync();

                await _loggingService.LogAsync("DEACTIVATE_CUSTOMER", "Customer", customerId,
                    $"Müşteri pasif hale getirildi: {customer.CompanyName}", userId: deactivatedByUserId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating customer: {CustomerId}", customerId);
                return ServiceResult<bool>.Failure("Müşteri pasifleştirme sırasında hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteri verilerini doğrular
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateCustomerDataAsync(CustomerDto customerDto)
        {
            var errors = new List<string>();

            // **REQUIRED FIELDS**
            if (string.IsNullOrWhiteSpace(customerDto.CompanyName))
                errors.Add("Şirket adı gereklidir.");

            // **BUSINESS VALIDATIONS**
            if (!string.IsNullOrEmpty(customerDto.TaxNumber))
            {
                if (customerDto.TaxNumber.Length < 10 || customerDto.TaxNumber.Length > 11)
                    errors.Add("Vergi numarası 10-11 haneli olmalıdır.");

                if (!customerDto.TaxNumber.All(char.IsDigit))
                    errors.Add("Vergi numarası sadece rakam içermelidir.");
            }

            if (!string.IsNullOrEmpty(customerDto.Email))
            {
                if (!IsValidEmail(customerDto.Email))
                    errors.Add("Geçerli bir e-posta adresi giriniz.");
            }

            if (customerDto.CreditLimit.HasValue && customerDto.CreditLimit.Value < 0)
                errors.Add("Kredi limiti negatif olamaz.");

            if (customerDto.PaymentTermDays < 1 || customerDto.PaymentTermDays > 365)
                errors.Add("Ödeme vadesi 1-365 gün arasında olmalıdır.");

            if (errors.Any())
            {
                return ServiceResult<bool>.ValidationFailure(errors);
            }

            return ServiceResult<bool>.Success(true);
        }

        // **PRIVATE HELPER METHODS**

        private static CustomerDto MapToDto(Customer customer)
        {
            return new CustomerDto
            {
                Id = customer.Id,
                CompanyName = customer.CompanyName,
                TaxNumber = customer.TaxNumber,
                ContactPerson = customer.ContactPerson,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                MobileNumber = customer.MobileNumber,
                Address = customer.Address,
                City = customer.City,
                District = customer.District,
                PostalCode = customer.PostalCode,
                CustomerType = customer.CustomerType,
                IsActive = customer.IsActive,
                Notes = customer.Notes,
                CreditLimit = customer.CreditLimit,
                PaymentTermDays = customer.PaymentTermDays,
                CreatedDate = customer.CreatedDate,
                UpdatedDate = customer.UpdatedDate
            };
        }

        private static bool IsValidEmail(string email)
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
