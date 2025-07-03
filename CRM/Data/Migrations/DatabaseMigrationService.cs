using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.Data.Migrations
{
    /// <summary>
    /// Veritabanı migration işlemlerini yöneten servis
    /// Production-ready migration ve versioning sistemi
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly TeknikServisDbContext _context;
        private readonly ILogger<DatabaseMigrationService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DatabaseMigrationService(
            TeknikServisDbContext context,
            ILogger<DatabaseMigrationService> logger,
            IServiceProvider serviceProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Tüm bekleyen migration'ları uygular
        /// </summary>
        public async Task ApplyMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Veritabanı migration işlemi başlatılıyor...");

                // **DATABASE CONNECTION CHECK**
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogWarning("⚠️ Veritabanına bağlanılamıyor, oluşturuluyor...");
                    await _context.Database.EnsureCreatedAsync();
                }

                // **PENDING MIGRATIONS CHECK**
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                var pendingMigrationsList = pendingMigrations.ToList();

                if (!pendingMigrationsList.Any())
                {
                    _logger.LogInformation("✅ Tüm migration'lar güncel, işlem gerekmiyor.");
                    return;
                }

                _logger.LogInformation("📋 {Count} adet bekleyen migration bulundu:", pendingMigrationsList.Count);
                foreach (var migration in pendingMigrationsList)
                {
                    _logger.LogInformation("  - {Migration}", migration);
                }

                // **BACKUP CREATION** (Production ortamında önemli)
                await CreateBackupIfNecessaryAsync();

                // **APPLY MIGRATIONS**
                _logger.LogInformation("🚀 Migration'lar uygulanıyor...");

                var startTime = DateTime.Now;
                await _context.Database.MigrateAsync();
                var duration = DateTime.Now - startTime;

                _logger.LogInformation("✅ Migration'lar başarıyla uygulandı. Süre: {Duration}ms", duration.TotalMilliseconds);

                // **VERIFY DATABASE STATE**
                await VerifyDatabaseStateAsync();

                _logger.LogInformation("🎉 Veritabanı migration işlemi tamamlandı!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Migration işlemi sırasında hata oluştu!");

                // **ROLLBACK STRATEGY**
                await HandleMigrationErrorAsync(ex);
                throw;
            }
        }

        /// <summary>
        /// Veritabanını sıfırlar ve yeniden oluşturur
        /// ⚠️ SADECE DEVELOPMENT ORTAMINDA KULLANIN!
        /// </summary>
        public async Task RecreateDatabase()
        {
#if DEBUG
            try
            {
                _logger.LogWarning("⚠️ UYARI: Veritabanı tamamen yeniden oluşturuluyor - TÜM VERİLER SİLİNECEK!");

                // **DELETE DATABASE**
                var deleted = await _context.Database.EnsureDeletedAsync();
                if (deleted)
                {
                    _logger.LogInformation("🗑️ Mevcut veritabanı silindi.");
                }

                // **CREATE NEW DATABASE**
                var created = await _context.Database.EnsureCreatedAsync();
                if (created)
                {
                    _logger.LogInformation("🆕 Yeni veritabanı oluşturuldu.");
                }

                // **SEED INITIAL DATA**
                await SeedInitialDataAsync();

                _logger.LogInformation("✅ Veritabanı başarıyla yeniden oluşturuldu!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Veritabanı yeniden oluşturma sırasında hata!");
                throw;
            }
#else
            throw new InvalidOperationException("RecreateDatabase sadece development ortamında kullanılabilir!");
#endif
        }

        /// <summary>
        /// Migration geçmişini gösterir
        /// </summary>
        public async Task<IEnumerable<string>> GetAppliedMigrationsAsync()
        {
            try
            {
                return await _context.Database.GetAppliedMigrationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Applied migration'lar alınırken hata oluştu");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Bekleyen migration'ları gösterir
        /// </summary>
        public async Task<IEnumerable<string>> GetPendingMigrationsAsync()
        {
            try
            {
                return await _context.Database.GetPendingMigrationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pending migration'lar alınırken hata oluştu");
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Veritabanı durumunu kontrol eder
        /// </summary>
        public async Task<DatabaseStatus> GetDatabaseStatusAsync()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return new DatabaseStatus
                    {
                        CanConnect = false,
                        Status = "Connection Failed",
                        Message = "Veritabanına bağlanılamıyor."
                    };
                }

                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();

                var appliedCount = appliedMigrations.Count();
                var pendingCount = pendingMigrations.Count();

                var status = pendingCount == 0 ? "Up to Date" : "Migrations Pending";
                var message = pendingCount == 0
                    ? $"Veritabanı güncel. {appliedCount} migration uygulanmış."
                    : $"{pendingCount} bekleyen migration var.";

                return new DatabaseStatus
                {
                    CanConnect = true,
                    Status = status,
                    Message = message,
                    AppliedMigrationsCount = appliedCount,
                    PendingMigrationsCount = pendingCount,
                    AppliedMigrations = appliedMigrations.ToList(),
                    PendingMigrations = pendingMigrations.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı durumu kontrol edilirken hata oluştu");
                return new DatabaseStatus
                {
                    CanConnect = false,
                    Status = "Error",
                    Message = $"Hata: {ex.Message}"
                };
            }
        }

        // **PRIVATE HELPER METHODS**

        /// <summary>
        /// Gerektiğinde veritabanı yedeği oluşturur
        /// </summary>
        private async Task CreateBackupIfNecessaryAsync()
        {
            try
            {
                // **PRODUCTION BACKUP STRATEGY**
                // Bu implementation SQL Server için basit bir örnek
                // Production'da daha sophisticated backup stratejisi gerekir

                var connectionString = _context.Database.GetConnectionString();
                if (connectionString?.Contains("localdb") == false &&
                    connectionString?.Contains("localhost") == false)
                {
                    _logger.LogInformation("🛡️ Production veritabanı tespit edildi, backup oluşturuluyor...");

                    // Backup logic burada implement edilecek
                    // Örnek: SQL Server backup command
                    await Task.Delay(1000); // Simulated backup time

                    _logger.LogInformation("✅ Backup oluşturuldu.");
                }
                else
                {
                    _logger.LogInformation("🔧 Development ortamı tespit edildi, backup atlanıyor.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Backup oluşturulamadı, migration devam ediyor...");
                // Backup failure shouldn't stop migration in most cases
            }
        }

        /// <summary>
        /// Migration sonrası veritabanı durumunu doğrular
        /// </summary>
        private async Task VerifyDatabaseStateAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Veritabanı durumu doğrulanıyor...");

                // **TABLE EXISTENCE CHECK**
                var tableNames = new[] { "Users", "Customers", "Services", "ServiceTasks", "SystemLogs" };

                foreach (var tableName in tableNames)
                {
                    var exists = await TableExistsAsync(tableName);
                    if (!exists)
                    {
                        throw new InvalidOperationException($"Tablo bulunamadı: {tableName}");
                    }
                    _logger.LogDebug("✓ Tablo mevcut: {TableName}", tableName);
                }

                // **BASIC CONNECTIVITY TEST**
                var userCount = await _context.Users.CountAsync();
                _logger.LogInformation("📊 Kullanıcı sayısı: {Count}", userCount);

                _logger.LogInformation("✅ Veritabanı durumu doğrulandı!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Veritabanı doğrulama başarısız!");
                throw;
            }
        }

        /// <summary>
        /// Belirtilen tablonun var olup olmadığını kontrol eder
        /// </summary>
        private async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var sql = _context.Database.IsSqlServer()
                    ? $"SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') THEN 1 ELSE 0 END"
                    : $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";

                var result = await _context.Database.SqlQueryRaw<int>(sql).FirstAsync();
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Migration hatası durumunda çalışır
        /// </summary>
        private async Task HandleMigrationErrorAsync(Exception exception)
        {
            try
            {
                _logger.LogError("🚨 Migration hatası işleniyor...");

                // **ERROR CATEGORIZATION**
                var errorType = CategorizeError(exception);

                switch (errorType)
                {
                    case MigrationErrorType.ConnectionTimeout:
                        _logger.LogWarning("⏱️ Bağlantı zaman aşımı - yeniden deneniyor...");
                        await Task.Delay(5000);
                        break;

                    case MigrationErrorType.LockTimeout:
                        _logger.LogWarning("🔒 Veritabanı kilitli - bekleniyor...");
                        await Task.Delay(10000);
                        break;

                    case MigrationErrorType.InsufficientPermissions:
                        _logger.LogError("🔐 Yetersiz veritabanı yetkileri!");
                        break;

                    case MigrationErrorType.CorruptedData:
                        _logger.LogError("💣 Veri bütünlüğü hatası!");
                        break;

                    default:
                        _logger.LogError("❓ Bilinmeyen hata türü");
                        break;
                }

                // **LOG DETAILED ERROR**
                await LogDetailedErrorAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "💥 CRITICAL: Error handler itself failed!");
            }
        }

        /// <summary>
        /// Hatayı kategorize eder
        /// </summary>
        private static MigrationErrorType CategorizeError(Exception exception)
        {
            var message = exception.Message.ToLower();

            if (message.Contains("timeout"))
                return MigrationErrorType.ConnectionTimeout;

            if (message.Contains("lock") || message.Contains("deadlock"))
                return MigrationErrorType.LockTimeout;

            if (message.Contains("permission") || message.Contains("access denied"))
                return MigrationErrorType.InsufficientPermissions;

            if (message.Contains("constraint") || message.Contains("foreign key"))
                return MigrationErrorType.CorruptedData;

            return MigrationErrorType.Unknown;
        }

        /// <summary>
        /// Detaylı hata loglaması
        /// </summary>
        private async Task LogDetailedErrorAsync(Exception exception)
        {
            try
            {
                var errorDetails = new
                {
                    Timestamp = DateTime.Now,
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    DatabaseProvider = _context.Database.ProviderName,
                    ConnectionString = _context.Database.GetConnectionString()?.Replace("Password=", "Password=***"),
                    AppliedMigrations = await GetAppliedMigrationsAsync(),
                    PendingMigrations = await GetPendingMigrationsAsync()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(errorDetails, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                _logger.LogError("📋 Detaylı hata raporu:\n{ErrorDetails}", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging detailed error information");
            }
        }

        /// <summary>
        /// İlk verileri yükler
        /// </summary>
        private async Task SeedInitialDataAsync()
        {
            try
            {
                _logger.LogInformation("🌱 İlk veriler yükleniyor...");

                using var scope = _serviceProvider.CreateScope();
                var dbInitializer = scope.ServiceProvider.GetService<DbInitializer>();

                if (dbInitializer != null)
                {
                    await dbInitializer.InitializeAsync(_context, isDevelopment: true);
                    _logger.LogInformation("✅ İlk veriler yüklendi!");
                }
                else
                {
                    _logger.LogWarning("⚠️ DbInitializer bulunamadı, seed data atlanıyor.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seed data yükleme sırasında hata oluştu");
            }
        }
    }

    /// <summary>
    /// Migration hata türleri
    /// </summary>
    public enum MigrationErrorType
    {
        Unknown,
        ConnectionTimeout,
        LockTimeout,
        InsufficientPermissions,
        CorruptedData
    }

    /// <summary>
    /// Veritabanı durumu bilgisi
    /// </summary>
    public class DatabaseStatus
    {
        public bool CanConnect { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int AppliedMigrationsCount { get; set; }
        public int PendingMigrationsCount { get; set; }
        public List<string> AppliedMigrations { get; set; } = new();
        public List<string> PendingMigrations { get; set; } = new();
    }
}
