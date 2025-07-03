using Microsoft.Extensions.Logging;

namespace CRM.Services.CrossCutting
{
    public class BackgroundTaskService : IBackgroundTaskService
    {
        private readonly ILogger<BackgroundTaskService> _logger;
        private readonly Timer _timer;

        public BackgroundTaskService(ILogger<BackgroundTaskService> logger)
        {
            _logger = logger;
            _timer = new Timer(ProcessTasks, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        }

        public async Task ScheduleTaskAsync(Func<Task> task, TimeSpan delay)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    await task();
                });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling task");
            }
        }

        public async Task ScheduleRecurringTaskAsync(Func<Task> task, TimeSpan interval)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(interval);
                        await task();
                    }
                });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling recurring task");
            }
        }

        public async Task ProcessPendingTasksAsync()
        {
            try
            {
                // Implementation: Process queued background tasks
                _logger.LogDebug("Processing pending background tasks");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending tasks");
            }
        }

        private void ProcessTasks(object? state)
        {
            _ = Task.Run(ProcessPendingTasksAsync);
        }
    }
}
