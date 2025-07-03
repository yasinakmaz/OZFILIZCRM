namespace CRM.Services.Utilities
{
    public interface IPdfService
    {
        Task<byte[]> GenerateServiceReportAsync(int serviceId);
        Task<byte[]> GenerateInvoiceAsync(int serviceId);
    }
}
