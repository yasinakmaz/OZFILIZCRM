namespace CRM.Services.Utilities
{
    public interface INotificationService
    {
        Task SendServiceAssignedNotificationAsync(int serviceId, int assignedUserId);
        Task SendServiceStatusChangedNotificationAsync(int serviceId, int userId, ServiceStatus oldStatus, ServiceStatus newStatus);
        Task SendNewServiceNotificationAsync(int serviceId, int userId);
        Task SendCustomerServiceNotificationAsync(int serviceId, int customerId, ServiceStatus status);
        Task SendTaskCompletedNotificationAsync(int taskId, int userId);
        Task SendOverdueServiceNotificationAsync(int serviceId, int userId);
    }
}
