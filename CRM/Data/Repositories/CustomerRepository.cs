using Microsoft.Extensions.Logging;

namespace CRM.Data.Repositories
{
    /// <summary>
    /// Customer repository implementation - CRM functionality için specific operations
    /// </summary>
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(TeknikServisDbContext context, ILogger<CustomerRepository> logger)
            : base(context, logger)
        {
        }

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            try
            {
                var normalizedSearch = searchTerm.ToLower();
                return await _dbSet
                    .Where(c => c.IsActive &&
                               (c.CompanyName.ToLower().Contains(normalizedSearch) ||
                                c.ContactPerson != null && c.ContactPerson.ToLower().Contains(normalizedSearch) ||
                                c.Email != null && c.Email.ToLower().Contains(normalizedSearch) ||
                                c.TaxNumber != null && c.TaxNumber.Contains(searchTerm)))
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<Customer?> GetByCompanyNameAsync(string companyName)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(c => c.CompanyName == companyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by company name: {CompanyName}", companyName);
                throw;
            }
        }

        public async Task<Customer?> GetByTaxNumberAsync(string taxNumber)
        {
            try
            {
                return await _dbSet.FirstOrDefaultAsync(c => c.TaxNumber == taxNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by tax number: {TaxNumber}", taxNumber);
                throw;
            }
        }

        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
        {
            try
            {
                return await _dbSet
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active customers");
                throw;
            }
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithPendingServicesAsync()
        {
            try
            {
                return await _dbSet
                    .Include(c => c.Services)
                    .Where(c => c.IsActive && c.Services.Any(s => s.Status == ServiceStatus.Pending || s.Status == ServiceStatus.InProgress))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers with pending services");
                throw;
            }
        }

        public async Task<(IEnumerable<Customer> Customers, int TotalCount)> GetCustomersPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
        {
            try
            {
                IQueryable<Customer> query = _dbSet.Where(c => c.IsActive);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var normalizedSearch = searchTerm.ToLower();
                    query = query.Where(c => c.CompanyName.ToLower().Contains(normalizedSearch) ||
                                           (c.ContactPerson != null && c.ContactPerson.ToLower().Contains(normalizedSearch)) ||
                                           (c.Email != null && c.Email.ToLower().Contains(normalizedSearch)));
                }

                var totalCount = await query.CountAsync();
                var customers = await query
                    .OrderBy(c => c.CompanyName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (customers, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers paged");
                throw;
            }
        }

        public async Task<bool> HasActiveServicesAsync(int customerId)
        {
            try
            {
                return await _context.Services
                    .AnyAsync(s => s.CustomerId == customerId &&
                                  (s.Status == ServiceStatus.Pending ||
                                   s.Status == ServiceStatus.InProgress ||
                                   s.Status == ServiceStatus.Accepted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active services for customer: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<decimal> GetTotalServiceAmountAsync(int customerId)
        {
            try
            {
                return await _context.Services
                    .Where(s => s.CustomerId == customerId && s.ServiceAmount.HasValue)
                    .SumAsync(s => s.ServiceAmount!.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total service amount for customer: {CustomerId}", customerId);
                throw;
            }
        }
    }
}
