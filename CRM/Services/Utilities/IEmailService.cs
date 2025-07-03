namespace CRM.Services.Utilities
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendServiceNotificationEmailAsync(int serviceId, string customerEmail);
    }
}
