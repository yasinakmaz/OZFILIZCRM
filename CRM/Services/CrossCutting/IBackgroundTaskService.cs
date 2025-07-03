namespace CRM.Services.CrossCutting
{
    public interface IBackgroundTaskService
    {
        Task ScheduleTaskAsync(Func<Task> task, TimeSpan delay);
        Task ScheduleRecurringTaskAsync(Func<Task> task, TimeSpan interval);
        Task ProcessPendingTasksAsync();
    }
}
