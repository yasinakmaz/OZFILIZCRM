namespace CRM.Services.Utilities
{
    public interface IReportService
    {
        Task<ServiceResult<byte[]>> GenerateServiceReportAsync(DateTime startDate, DateTime endDate);
        Task<ServiceResult<byte[]>> GenerateCustomerReportAsync(int customerId);
        Task<ServiceResult<byte[]>> GenerateUserPerformanceReportAsync(int userId);
    }
}
