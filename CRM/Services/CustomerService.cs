namespace CRM.Services
{
    /// <summary>
    /// Müşteri yönetimi için business logic servisi
    /// CRUD işlemleri, validation ve business rules içerir
    /// </summary>
    public class CustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly LoggingService _loggingService;

        public CustomerService(ICustomerRepository customerRepository, LoggingService loggingService)
        {
            _customerRepository = customerRepository;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Sayfalama ile müşteri listesini getirir
        /// Arama ve filtreleme desteği vardır
        /// </summary>
        public async Task<(IEnumerable<Customer> customers, int totalCount)> GetCustomersPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CompanyType? companyType = null,
            bool includeInactive = false)
        {
            try
            {
                if (!includeInactive)
                {
                    // Sadece aktif müşteriler
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        var searchResults = await _customerRepository.SearchCustomersAsync(searchTerm);
                        var totalCount = searchResults.Count();

                        var pagedResults = searchResults
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

                        return (pagedResults, totalCount);
                    }
                    else if (companyType.HasValue)
                    {
                        var typeResults = await _customerRepository.GetByCompanyTypeAsync(companyType.Value);
                        var totalCount = typeResults.Count();

                        var pagedResults = typeResults
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

                        return (pagedResults, totalCount);
                    }
                }

                return await _customerRepository.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    predicate: includeInactive ? null : c => c.IsActive,
                    orderBy: c => c.CompanyName);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GET_CUSTOMERS_PAGED", "Customer");
                throw;
            }
        }

        /// <summary>
        /// ID'ye göre müşteri getirir
        /// Servis geçmişi ile birlikte detaylı bilgi
        /// </summary>
        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            try
            {
                return await _customerRepository.GetCustomerWithServicesAsync(customerId);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GET_CUSTOMER_BY_ID", "Customer", customerId);
                throw;
            }
        }

        /// <summary>
        /// Müşteri arama işlemi
        /// Firma adı, yetkili kişi, telefon ve vergi numarasında arama yapar
        /// </summary>
        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await _customerRepository.GetActiveCustomersAsync();

                return await _customerRepository.SearchCustomersAsync(searchTerm);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "SEARCH_CUSTOMERS", "Customer");
                throw;
            }
        }

        /// <summary>
        /// Yeni müşteri oluşturur
        /// Validation ve business rules kontrolü yapar
        /// </summary>
        public async Task<ServiceResult<Customer>> CreateCustomerAsync(CustomerDto customerDto, int userId)
        {
            try
            {
                // Validation kontrolü
                var validationResult = await ValidateCustomerAsync(customerDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<Customer>.Failure(validationResult.ErrorMessage!);
                }

                // Vergi numarası benzersizlik kontrolü
                var isUnique = await _customerRepository.IsTaxNumberUniqueAsync(customerDto.TaxNumber);
                if (!isUnique)
                {
                    return ServiceResult<Customer>.Failure("Bu vergi numarası/TCKN ile kayıtlı bir müşteri zaten mevcut.");
                }

                // Entity oluştur
                var customer = new Customer
                {
                    CompanyName = customerDto.CompanyName.Trim(),
                    CompanyType = customerDto.CompanyType,
                    TaxNumber = customerDto.TaxNumber.Trim(),
                    TaxOffice = customerDto.TaxOffice?.Trim(),
                    AuthorizedPersonName = customerDto.AuthorizedPersonName.Trim(),
                    PhoneNumber = customerDto.PhoneNumber.Trim(),
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                var createdCustomer = await _customerRepository.AddAsync(customer);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "CREATE_CUSTOMER",
                    "Customer",
                    createdCustomer.Id,
                    $"Yeni müşteri oluşturuldu: {createdCustomer.CompanyName}",
                    userId: userId,
                    newValues: createdCustomer);

                return ServiceResult<Customer>.Success(createdCustomer);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "CREATE_CUSTOMER", "Customer", userId: userId);
                return ServiceResult<Customer>.Failure("Müşteri oluşturulurken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteri bilgilerini günceller
        /// Mevcut kayıt kontrolü ve validation yapar
        /// </summary>
        public async Task<ServiceResult<Customer>> UpdateCustomerAsync(int customerId, CustomerDto customerDto, int userId)
        {
            try
            {
                var existingCustomer = await _customerRepository.GetByIdAsync(customerId);
                if (existingCustomer == null)
                {
                    return ServiceResult<Customer>.Failure("Müşteri bulunamadı.");
                }

                // Validation kontrolü
                var validationResult = await ValidateCustomerAsync(customerDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<Customer>.Failure(validationResult.ErrorMessage!);
                }

                // Vergi numarası benzersizlik kontrolü (mevcut kayıt hariç)
                var isUnique = await _customerRepository.IsTaxNumberUniqueAsync(customerDto.TaxNumber, customerId);
                if (!isUnique)
                {
                    return ServiceResult<Customer>.Failure("Bu vergi numarası/TCKN ile kayıtlı başka bir müşteri mevcut.");
                }

                // Eski değerleri sakla (audit log için)
                var oldValues = new
                {
                    existingCustomer.CompanyName,
                    existingCustomer.CompanyType,
                    existingCustomer.TaxNumber,
                    existingCustomer.TaxOffice,
                    existingCustomer.AuthorizedPersonName,
                    existingCustomer.PhoneNumber
                };

                // Güncelleme işlemi
                existingCustomer.CompanyName = customerDto.CompanyName.Trim();
                existingCustomer.CompanyType = customerDto.CompanyType;
                existingCustomer.TaxNumber = customerDto.TaxNumber.Trim();
                existingCustomer.TaxOffice = customerDto.TaxOffice?.Trim();
                existingCustomer.AuthorizedPersonName = customerDto.AuthorizedPersonName.Trim();
                existingCustomer.PhoneNumber = customerDto.PhoneNumber.Trim();
                existingCustomer.UpdatedDate = DateTime.Now;

                var updatedCustomer = await _customerRepository.UpdateAsync(existingCustomer);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "UPDATE_CUSTOMER",
                    "Customer",
                    updatedCustomer.Id,
                    $"Müşteri güncellendi: {updatedCustomer.CompanyName}",
                    userId: userId,
                    oldValues: oldValues,
                    newValues: new
                    {
                        updatedCustomer.CompanyName,
                        updatedCustomer.CompanyType,
                        updatedCustomer.TaxNumber,
                        updatedCustomer.TaxOffice,
                        updatedCustomer.AuthorizedPersonName,
                        updatedCustomer.PhoneNumber
                    });

                return ServiceResult<Customer>.Success(updatedCustomer);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "UPDATE_CUSTOMER", "Customer", customerId, userId: userId);
                return ServiceResult<Customer>.Failure("Müşteri güncellenirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteriyi pasif hale getirir (soft delete)
        /// Müşteriye ait aktif servis varsa silme işlemine izin vermez
        /// </summary>
        public async Task<ServiceResult<bool>> DeactivateCustomerAsync(int customerId, int userId)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerWithServicesAsync(customerId);
                if (customer == null)
                {
                    return ServiceResult<bool>.Failure("Müşteri bulunamadı.");
                }

                // Aktif servis kontrolü
                var activeServices = customer.Services.Where(s =>
                    s.Status == ServiceStatus.Pending ||
                    s.Status == ServiceStatus.InProgress).ToList();

                if (activeServices.Any())
                {
                    return ServiceResult<bool>.Failure(
                        $"Bu müşterinin {activeServices.Count} adet aktif servisi bulunuyor. " +
                        "Önce servisleri tamamlayın veya iptal edin.");
                }

                customer.IsActive = false;
                customer.UpdatedDate = DateTime.Now;
                await _customerRepository.UpdateAsync(customer);

                // İşlemi logla
                await _loggingService.LogAsync(
                    "DEACTIVATE_CUSTOMER",
                    "Customer",
                    customerId,
                    $"Müşteri pasif hale getirildi: {customer.CompanyName}",
                    userId: userId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "DEACTIVATE_CUSTOMER", "Customer", customerId, userId: userId);
                return ServiceResult<bool>.Failure("Müşteri pasif hale getirilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteriyi tekrar aktif hale getirir
        /// </summary>
        public async Task<ServiceResult<bool>> ActivateCustomerAsync(int customerId, int userId)
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

                // İşlemi logla
                await _loggingService.LogAsync(
                    "ACTIVATE_CUSTOMER",
                    "Customer",
                    customerId,
                    $"Müşteri aktif hale getirildi: {customer.CompanyName}",
                    userId: userId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "ACTIVATE_CUSTOMER", "Customer", customerId, userId: userId);
                return ServiceResult<bool>.Failure("Müşteri aktif hale getirilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Müşteri istatistiklerini getirir
        /// Dashboard ve raporlama için kullanılır
        /// </summary>
        public async Task<CustomerStatsDto> GetCustomerStatsAsync(int customerId)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerWithServicesAsync(customerId);
                if (customer == null)
                {
                    return new CustomerStatsDto();
                }

                var stats = new CustomerStatsDto
                {
                    TotalServices = customer.Services.Count,
                    CompletedServices = customer.Services.Count(s => s.Status == ServiceStatus.Completed),
                    ActiveServices = customer.Services.Count(s =>
                        s.Status == ServiceStatus.Pending || s.Status == ServiceStatus.InProgress),
                    TotalAmount = customer.Services
                        .Where(s => s.ServiceAmount.HasValue && s.Status == ServiceStatus.Completed)
                        .Sum(s => s.ServiceAmount!.Value),
                    LastServiceDate = customer.Services
                        .OrderByDescending(s => s.CreatedDate)
                        .FirstOrDefault()?.CreatedDate,
                    AverageServiceAmount = customer.Services
                        .Where(s => s.ServiceAmount.HasValue && s.Status == ServiceStatus.Completed)
                        .Average(s => s.ServiceAmount!.Value) ?? 0
                };

                return stats;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GET_CUSTOMER_STATS", "Customer", customerId);
                return new CustomerStatsDto();
            }
        }

        /// <summary>
        /// Müşteri validation işlemi
        /// Business rules ve data validation kontrolü
        /// </summary>
        private async Task<ServiceResult<bool>> ValidateCustomerAsync(CustomerDto customerDto)
        {
            // Required field kontrolü
            if (string.IsNullOrWhiteSpace(customerDto.CompanyName))
                return ServiceResult<bool>.Failure("Firma adı gereklidir.");

            if (string.IsNullOrWhiteSpace(customerDto.TaxNumber))
                return ServiceResult<bool>.Failure("Vergi numarası/TCKN gereklidir.");

            if (string.IsNullOrWhiteSpace(customerDto.AuthorizedPersonName))
                return ServiceResult<bool>.Failure("Yetkili kişi adı gereklidir.");

            if (string.IsNullOrWhiteSpace(customerDto.PhoneNumber))
                return ServiceResult<bool>.Failure("Telefon numarası gereklidir.");

            // Vergi numarası format kontrolü
            if (!IsValidTaxNumber(customerDto.TaxNumber, customerDto.CompanyType))
            {
                return ServiceResult<bool>.Failure(
                    customerDto.CompanyType == CompanyType.Individual
                        ? "Geçerli bir TCKN giriniz (11 haneli)."
                        : "Geçerli bir vergi numarası giriniz (10 haneli).");
            }

            // Tüzel kişi için vergi dairesi zorunlu
            if (customerDto.CompanyType == CompanyType.Corporate && string.IsNullOrWhiteSpace(customerDto.TaxOffice))
            {
                return ServiceResult<bool>.Failure("Tüzel kişi müşteriler için vergi dairesi gereklidir.");
            }

            // Telefon numarası format kontrolü
            if (!IsValidPhoneNumber(customerDto.PhoneNumber))
            {
                return ServiceResult<bool>.Failure("Geçerli bir telefon numarası giriniz.");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// Vergi numarası/TCKN format kontrolü
        /// </summary>
        private bool IsValidTaxNumber(string taxNumber, CompanyType companyType)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
                return false;

            var cleanNumber = taxNumber.Trim().Replace(" ", "").Replace("-", "");

            if (companyType == CompanyType.Individual)
            {
                // TCKN kontrolü - 11 hane olmalı ve sadece rakam
                return cleanNumber.Length == 11 && cleanNumber.All(char.IsDigit);
            }
            else
            {
                // Vergi numarası kontrolü - 10 hane olmalı ve sadece rakam
                return cleanNumber.Length == 10 && cleanNumber.All(char.IsDigit);
            }
        }

        /// <summary>
        /// Telefon numarası format kontrolü
        /// </summary>
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var cleanNumber = phoneNumber.Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("+90", "");

            // 10 haneli telefon numarası (0 ile başlayan 11 haneli de kabul edilir)
            return (cleanNumber.Length == 10 || cleanNumber.Length == 11) &&
                   cleanNumber.All(char.IsDigit);
        }
    }

    /// <summary>
    /// Müşteri istatistikleri için DTO
    /// </summary>
    public class CustomerStatsDto
    {
        public int TotalServices { get; set; }
        public int CompletedServices { get; set; }
        public int ActiveServices { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageServiceAmount { get; set; }
        public DateTime? LastServiceDate { get; set; }
    }

    /// <summary>
    /// Generic service result wrapper
    /// API response standardization için kullanılır
    /// </summary>
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T> { IsSuccess = true, Data = data };
        }

        public static ServiceResult<T> Failure(string errorMessage)
        {
            return new ServiceResult<T> { IsSuccess = false, ErrorMessage = errorMessage };
        }
    }
}
