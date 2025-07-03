namespace CRM.Services
{
    /// <summary>
    /// Müşteri servisi interface'i
    /// Customer management ve CRM functionality
    /// </summary>
    public interface ICustomerService
    {
        Task<ServiceResult<CustomerDto>> CreateCustomerAsync(CustomerDto customerDto, int createdByUserId);
        Task<ServiceResult<CustomerDto>> UpdateCustomerAsync(CustomerDto customerDto, int updatedByUserId);
        Task<ServiceResult<bool>> DeleteCustomerAsync(int customerId, int deletedByUserId);
        Task<ServiceResult<CustomerDto>> GetCustomerByIdAsync(int customerId);
        Task<ServiceResult<PagedResultDto<CustomerDto>>> GetCustomersPagedAsync(int pageNumber, int pageSize, string? searchTerm = null);
        Task<ServiceResult<IEnumerable<CustomerSearchDto>>> SearchCustomersAsync(string searchTerm);
        Task<ServiceResult<CustomerStatsDto>> GetCustomerStatsAsync();
        Task<ServiceResult<bool>> ActivateCustomerAsync(int customerId, int activatedByUserId);
        Task<ServiceResult<bool>> DeactivateCustomerAsync(int customerId, int deactivatedByUserId);
        Task<ServiceResult<bool>> ValidateCustomerDataAsync(CustomerDto customerDto);
    }
}
