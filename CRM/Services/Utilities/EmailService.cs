using Microsoft.Extensions.Logging;

namespace CRM.Services.Utilities
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                // Implementation: SMTP, SendGrid, etc.
                _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
                await Task.Delay(100); // Simulate sending
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}", to);
                return false;
            }
        }

        public async Task<bool> SendServiceNotificationEmailAsync(int serviceId, string customerEmail)
        {
            try
            {
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending service notification email");
                return false;
            }
        }
    }
}
