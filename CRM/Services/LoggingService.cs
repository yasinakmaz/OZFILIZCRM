using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CRM.Services
{
    /// <summary>
    /// Logging servisi implementation
    /// Comprehensive audit trail ve error logging
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly ISystemLogRepository _logRepository;
        private readonly ILogger<LoggingService> _logger;
        private readonly IDeviceInfo _deviceInfo;

        public LoggingService(
            ISystemLogRepository logRepository,
            ILogger<LoggingService> logger,
            IDeviceInfo deviceInfo)
        {
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
        }

        /// <summary>
        /// System event'ini loglar
        /// </summary>
        public async Task LogAsync(string action, string entityType, int? entityId, string description, LogLevel logLevel = LogLevel.Information, int? userId = null)
        {
            try
            {
                var systemLog = new SystemLog
                {
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    UserId = userId,
                    LogLevel = logLevel,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    CreatedDate = DateTime.Now
                };

                await _logRepository.AddAsync(systemLog);
                await _logRepository.SaveChangesAsync();

                // Ayrıca Serilog'a da yaz
                _logger.Log((Microsoft.Extensions.Logging.LogLevel)logLevel,
                    "{Action} - {EntityType}:{EntityId} - {Description} (User: {UserId})",
                    action, entityType, entityId, description, userId);
            }
            catch (Exception ex)
            {
                // Logging error'u log etmemek için sadece Serilog'a yaz
                _logger.LogError(ex, "Error writing to system log: {Action}", action);
            }
        }

        /// <summary>
        /// Exception'ı detaylı şekilde loglar
        /// </summary>
        public async Task LogErrorAsync(Exception exception, string action, string entityType, int? entityId = null, int? userId = null)
        {
            try
            {
                var systemLog = new SystemLog
                {
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = exception.Message,
                    UserId = userId,
                    LogLevel = LogLevel.Error,
                    ExceptionDetails = GetExceptionDetails(exception),
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    CreatedDate = DateTime.Now
                };

                await _logRepository.AddAsync(systemLog);
                await _logRepository.SaveChangesAsync();

                // Ayrıca Serilog'a da yaz
                _logger.LogError(exception, "{Action} - {EntityType}:{EntityId} (User: {UserId})",
                    action, entityType, entityId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error in error logging system!");
            }
        }

        /// <summary>
        /// Log kayıtlarını sayfalı şekilde getirir
        /// </summary>
        public async Task<IEnumerable<SystemLog>> GetLogsAsync(int pageNumber = 1, int pageSize = 50, LogLevel? minLogLevel = null)
        {
            try
            {
                var result = await _logRepository.GetLogsPagedAsync(
                    pageNumber, pageSize,
                    startDate: minLogLevel.HasValue ? DateTime.Now.AddDays(-30) : null);

                return result.Logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return Enumerable.Empty<SystemLog>();
            }
        }

        /// <summary>
        /// Belirli kullanıcının log kayıtlarını getirir
        /// </summary>
        public async Task<IEnumerable<SystemLog>> GetUserLogsAsync(int userId, int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                var result = await _logRepository.GetLogsPagedAsync(
                    pageNumber, pageSize, userId: userId);

                return result.Logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user logs for user: {UserId}", userId);
                return Enumerable.Empty<SystemLog>();
            }
        }

        /// <summary>
        /// Belirli entity'nin log kayıtlarını getirir
        /// </summary>
        public async Task<IEnumerable<SystemLog>> GetEntityLogsAsync(string entityType, int entityId)
        {
            try
            {
                return await _logRepository.GetLogsByEntityAsync(entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity logs for {EntityType}:{EntityId}", entityType, entityId);
                return Enumerable.Empty<SystemLog>();
            }
        }

        /// <summary>
        /// Eski log kayıtlarını temizler
        /// </summary>
        public async Task CleanupOldLogsAsync(int daysToKeep = 90)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                await _logRepository.CleanupOldLogsAsync(cutoffDate);

                _logger.LogInformation("Log cleanup completed. Removed logs older than {CutoffDate}", cutoffDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log cleanup");
            }
        }

        /// <summary>
        /// Log boyutunu döndürür
        /// </summary>
        public async Task<long> GetLogsSizeAsync()
        {
            try
            {
                return await _logRepository.GetLogSizeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs size");
                return 0;
            }
        }

        // **PRIVATE HELPER METHODS**

        private string GetClientIpAddress()
        {
            try
            {
                // MAUI app'te gerçek IP alamayız, device bilgilerini kullanabiliriz
                return $"Device-{_deviceInfo.Model}-{_deviceInfo.Platform}";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetUserAgent()
        {
            try
            {
                return $"{_deviceInfo.Platform} {_deviceInfo.Version} - {_deviceInfo.Name}";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetExceptionDetails(Exception exception)
        {
            var details = new System.Text.StringBuilder();

            var ex = exception;
            var level = 0;

            while (ex != null && level < 5) // Max 5 level deep
            {
                if (level > 0) details.AppendLine($"--- Inner Exception Level {level} ---");

                details.AppendLine($"Type: {ex.GetType().FullName}");
                details.AppendLine($"Message: {ex.Message}");
                details.AppendLine($"Source: {ex.Source}");

                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    details.AppendLine("StackTrace:");
                    details.AppendLine(ex.StackTrace);
                }

                ex = ex.InnerException;
                level++;
            }

            return details.ToString();
        }
    }
}
