namespace CRM.Services.Utilities
{
    public interface IPrintService
    {
        Task<bool> PrintServiceReportAsync(int serviceId);
        Task<bool> PrintInvoiceAsync(int serviceId);
    }
}
