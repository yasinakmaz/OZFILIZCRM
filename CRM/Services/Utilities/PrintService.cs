using Microsoft.Extensions.Logging;

namespace CRM.Services.Utilities
{
    public class PrintService : IPrintService
    {
        private readonly ILogger<PrintService> _logger;

        public PrintService(ILogger<PrintService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> PrintServiceReportAsync(int serviceId)
        {
            try
            {
                _logger.LogInformation("Service report printed for service {ServiceId}", serviceId);
                await Task.Delay(200);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing service report");
                return false;
            }
        }

        public async Task<bool> PrintInvoiceAsync(int serviceId)
        {
            try
            {
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing invoice");
                return false;
            }
        }
    }
}
