namespace CRM.Services
{
    /// <summary>
    /// Logging servisi interface'i
    /// System events, audit trail ve error logging için
    /// </summary>
    public interface ILoggingService
    {
        Task LogAsync(string action, string entityType, int? entityId, string description, LogLevel logLevel = LogLevel.Information, int? userId = null);
        Task LogErrorAsync(Exception exception, string action, string entityType, int? entityId = null, int? userId = null);
        Task<IEnumerable<SystemLog>> GetLogsAsync(int pageNumber = 1, int pageSize = 50, LogLevel? minLogLevel = null);
        Task<IEnumerable<SystemLog>> GetUserLogsAsync(int userId, int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<SystemLog>> GetEntityLogsAsync(string entityType, int entityId);
        Task CleanupOldLogsAsync(int daysToKeep = 90);
        Task<long> GetLogsSizeAsync();
    }

}
