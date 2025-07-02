namespace CRM.Data.Repositories
{
    /// <summary>
    /// Customer entity'si için özelleştirilmiş repository interface
    /// Müşteri yönetimi ve arama işlemleri için özel metodlar içerir
    /// </summary>
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetByTaxNumberAsync(string taxNumber);
        Task<IEnumerable<Customer>> GetByCompanyTypeAsync(CompanyType companyType);
        Task<IEnumerable<Customer>> GetActiveCustomersAsync();
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
        Task<bool> IsTaxNumberUniqueAsync(string taxNumber, int? excludeCustomerId = null);
        Task<(IEnumerable<Customer> customers, int totalCount)> GetCustomersWithServicesAsync(
            int pageNumber, int pageSize, string? searchTerm = null);
        Task<Customer?> GetCustomerWithServicesAsync(int customerId);
        Task<IEnumerable<Customer>> GetTopCustomersByServiceCountAsync(int count = 10);
    }
}
