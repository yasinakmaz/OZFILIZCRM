using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CRM.Services
{
    /// <summary>
    /// Sistem geneli loglama servisi
    /// Hem veritabanına (audit trail için) hem de dosyaya (debugging için) log yapar
    /// Güvenlik, performans ve hata takibi için kritik öneme sahiptir
    /// </summary>
    public class LoggingService
    {
        private readonly TeknikServisDbContext _context;
        private readonly ILogger<LoggingService> _logger;

        /// <summary>
        /// Constructor - DI container'dan gerekli servisleri alır
        /// </summary>
        public LoggingService(TeknikServisDbContext context, ILogger<LoggingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Ana loglama metodu - hem veritabanına hem dosyaya yazar
        /// Tüm kritik sistem işlemleri bu metod üzerinden loglanmalıdır
        /// </summary>
        /// <param name="action">Gerçekleştirilen işlem (CREATE_SERVICE, LOGIN, UPDATE_CUSTOMER vs.)</param>
        /// <param name="entityType">İşlem uygulanan entity tipi (Service, Customer, User vs.)</param>
        /// <param name="entityId">İşlem uygulanan kaydın ID'si (opsiyonel)</param>
        /// <param name="description">İşlem açıklaması</param>
        /// <param name="ipAddress">İsteğin geldiği IP adresi (opsiyonel)</param>
        /// <param name="userId">İşlemi yapan kullanıcı ID'si (opsiyonel)</param>
        /// <param name="oldValues">İşlem öncesi değerler (JSON format, opsiyonel)</param>
        /// <param name="newValues">İşlem sonrası değerler (JSON format, opsiyonel)</param>
        public async Task LogAsync(
            string action,
            string entityType,
            int? entityId = null,
            string? description = null,
            string? ipAddress = null,
            int? userId = null,
            object? oldValues = null,
            object? newValues = null)
        {
            try
            {
                // Veritabanına audit log kaydı oluştur
                var auditLog = new AuditLog
                {
                    UserId = userId ?? 0, // 0 = System user (anonim işlemler için)
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    IpAddress = ipAddress,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    Timestamp = DateTime.Now
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                // Dosyaya da log yaz (Serilog üzerinden)
                var logMessage = $"Action: {action}, Entity: {entityType}" +
                    (entityId.HasValue ? $", EntityId: {entityId}" : "") +
                    (userId.HasValue ? $", UserId: {userId}" : "") +
                    (!string.IsNullOrEmpty(description) ? $", Description: {description}" : "");

                _logger.LogInformation(logMessage);
            }
            catch (Exception ex)
            {
                // Loglama işlemi başarısız olursa, bu hatayı da logla
                // Ancak veritabanı hatası olabileceği için sadece dosyaya yaz
                _logger.LogError(ex, "Log yazma işlemi başarısız: Action={Action}, EntityType={EntityType}",
                    action, entityType);
            }
        }

        /// <summary>
        /// Hata loglama için özelleştirilmiş metod
        /// Exception detayları ile birlikte log yapar
        /// </summary>
        public async Task LogErrorAsync(
            Exception exception,
            string action,
            string entityType,
            int? entityId = null,
            string? additionalInfo = null,
            int? userId = null)
        {
            var description = $"ERROR: {exception.Message}";
            if (!string.IsNullOrEmpty(additionalInfo))
                description += $" | Additional Info: {additionalInfo}";

            await LogAsync(
                action: $"ERROR_{action}",
                entityType: entityType,
                entityId: entityId,
                description: description,
                userId: userId,
                newValues: new
                {
                    ExceptionType = exception.GetType().Name,
                    ExceptionMessage = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                });

            // Dosyaya da detaylı hata logu yaz
            _logger.LogError(exception,
                "Action: {Action}, EntityType: {EntityType}, EntityId: {EntityId}, UserId: {UserId}, Info: {Info}",
                action, entityType, entityId, userId, additionalInfo);
        }

        /// <summary>
        /// Kullanıcı aktivitelerini loglar
        /// Login, logout, önemli işlemler için kullanılır
        /// </summary>
        public async Task LogUserActivityAsync(
            int userId,
            string activity,
            string? details = null,
            string? ipAddress = null)
        {
            await LogAsync(
                action: $"USER_{activity}",
                entityType: "User",
                entityId: userId,
                description: details,
                ipAddress: ipAddress,
                userId: userId);
        }

        /// <summary>
        /// Servis durumu değişikliklerini loglar
        /// Servis yaşam döngüsünü takip etmek için kritik
        /// </summary>
        public async Task LogServiceStatusChangeAsync(
            int serviceId,
            string oldStatus,
            string newStatus,
            int userId,
            string? reason = null)
        {
            await LogAsync(
                action: "STATUS_CHANGE",
                entityType: "Service",
                entityId: serviceId,
                description: reason,
                userId: userId,
                oldValues: new { Status = oldStatus },
                newValues: new { Status = newStatus });
        }

        /// <summary>
        /// Veri değişikliklerini detaylı olarak loglar
        /// Entity update işlemlerinde before/after değerleri kaydeder
        /// </summary>
        public async Task LogDataChangeAsync<T>(
            string action,
            int entityId,
            T oldEntity,
            T newEntity,
            int userId,
            string? description = null) where T : class
        {
            await LogAsync(
                action: action,
                entityType: typeof(T).Name,
                entityId: entityId,
                description: description,
                userId: userId,
                oldValues: oldEntity,
                newValues: newEntity);
        }

        /// <summary>
        /// Belirli tarih aralığındaki logları getirir
        /// Raporlama ve analiz için kullanılır
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetLogsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string? action = null,
            string? entityType = null,
            int? userId = null)
        {
            var query = _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.Timestamp >= startDate && al.Timestamp <= endDate);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(al => al.Action.Contains(action));

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(al => al.EntityType == entityType);

            if (userId.HasValue)
                query = query.Where(al => al.UserId == userId.Value);

            return await query
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir entity'nin tüm log geçmişini getirir
        /// Entity detail sayfalarında geçmiş takibi için kullanılır
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetEntityLogsAsync(
            string entityType,
            int entityId)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.EntityType == entityType && al.EntityId == entityId)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Log istatistiklerini getirir
        /// Dashboard'da sistem aktivite widget'ı için kullanılır
        /// </summary>
        public async Task<Dictionary<string, int>> GetLogStatisticsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(al => al.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.Timestamp <= endDate.Value);

            var stats = await query
                .GroupBy(al => al.Action)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return stats;
        }

        /// <summary>
        /// En aktif kullanıcıları getirir
        /// Performans analizi ve yönetim raporları için kullanılır
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetMostActiveUsersAsync(
            int count = 10,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.AuditLogs
                .Include(al => al.User)
                .Where(al => al.UserId > 0); // System user'ı hariç tut

            if (startDate.HasValue)
                query = query.Where(al => al.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(al => al.Timestamp <= endDate.Value);

            return await query
                .GroupBy(al => new { al.UserId, al.User.Username, al.User.Email })
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    Username = g.Key.Username,
                    Email = g.Key.Email,
                    ActivityCount = g.Count()
                })
                .OrderByDescending(x => x.ActivityCount)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Sistem sağlığı kontrolü için log temizleme
        /// Eski logları temizler (data retention policy)
        /// </summary>
        public async Task CleanupOldLogsAsync(int retentionDays = 365)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);

                var oldLogs = await _context.AuditLogs
                    .Where(al => al.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.AuditLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();

                    await LogAsync(
                        action: "CLEANUP_LOGS",
                        entityType: "AuditLog",
                        description: $"{oldLogs.Count} adet eski log kaydı temizlendi. Cutoff Date: {cutoffDate:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log temizleme işlemi başarısız");
            }
        }
    }
}
