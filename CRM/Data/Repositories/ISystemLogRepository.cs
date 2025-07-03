namespace CRM.Data.Repositories
{
    /// <summary>
    /// SystemLog entity için özel repository interface
    /// Logging ve audit trail operations
    /// </summary>
    public interface ISystemLogRepository : IRepository<SystemLog>
    {
        Task<IEnumerable<SystemLog>> GetLogsByUserAsync(int userId);
        Task<IEnumerable<SystemLog>> GetLogsByActionAsync(string action);
        Task<IEnumerable<SystemLog>> GetLogsByEntityAsync(string entityType, int entityId);
        Task<IEnumerable<SystemLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<SystemLog>> GetErrorLogsAsync();
        Task<(IEnumerable<SystemLog> Logs, int TotalCount)> GetLogsPagedAsync(
            int pageNumber, int pageSize,
            string? action = null,
            string? entityType = null,
            int? userId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
        Task CleanupOldLogsAsync(DateTime olderThan);
        Task<long> GetLogSizeAsync();
    }
}
