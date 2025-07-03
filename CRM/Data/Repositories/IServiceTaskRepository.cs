namespace CRM.Data.Repositories
{
    /// <summary>
    /// ServiceTask entity için özel repository interface
    /// Task management ve tracking operations
    /// </summary>
    public interface IServiceTaskRepository : IRepository<ServiceTask>
    {
        Task<IEnumerable<ServiceTask>> GetTasksByServiceAsync(int serviceId);
        Task<IEnumerable<ServiceTask>> GetTasksByUserAsync(int userId);
        Task<IEnumerable<ServiceTask>> GetPendingTasksAsync();
        Task<IEnumerable<ServiceTask>> GetCompletedTasksAsync();
        Task<IEnumerable<ServiceTask>> GetTasksByPriorityAsync(Priority priority);
        Task<bool> CompleteTaskAsync(int taskId, int completedByUserId);
        Task<int> GetCompletedTaskCountByServiceAsync(int serviceId);
        Task<int> GetTotalTaskCountByServiceAsync(int serviceId);
        Task<IEnumerable<ServiceTask>> GetOverdueTasksAsync();
        Task<double> GetServiceProgressPercentageAsync(int serviceId);
    }
}
