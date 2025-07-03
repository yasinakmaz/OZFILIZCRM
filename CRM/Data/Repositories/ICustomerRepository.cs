namespace CRM.Data.Repositories
{
    /// <summary>
    /// Customer entity için özel repository interface
    /// Customer management ve search operations
    /// </summary>
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
        Task<Customer?> GetByCompanyNameAsync(string companyName);
        Task<Customer?> GetByTaxNumberAsync(string taxNumber);
        Task<IEnumerable<Customer>> GetActiveCustomersAsync();
        Task<IEnumerable<Customer>> GetCustomersWithPendingServicesAsync();
        Task<(IEnumerable<Customer> Customers, int TotalCount)> GetCustomersPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null);
        Task<bool> HasActiveServicesAsync(int customerId);
        Task<decimal> GetTotalServiceAmountAsync(int customerId);
    }
}
