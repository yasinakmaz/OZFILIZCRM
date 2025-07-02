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
    /// Uygulama başlangıcında otomatik migration çalıştırır
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly TeknikServisDbContext _context;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(TeknikServisDbContext context, ILogger<DatabaseMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Bekleyen migration'ları uygular
        /// Production ortamında dikkatli kullanılmalıdır
        /// </summary>
        public async Task ApplyMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("Veritabanı migration kontrolü başlatılıyor...");

                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    _logger.LogInformation($"{pendingMigrations.Count()} adet bekleyen migration bulundu.");

                    foreach (var migration in pendingMigrations)
                    {
                        _logger.LogInformation($"Migration uygulanıyor: {migration}");
                    }

                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Tüm migration'lar başarıyla uygulandı.");
                }
                else
                {
                    _logger.LogInformation("Bekleyen migration bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration uygulanırken hata oluştu");
                throw;
            }
        }

        /// <summary>
        /// Veritabanı bağlantısını kontrol eder
        /// </summary>
        public async Task<bool> CanConnectAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantı kontrolü başarısız");
                return false;
            }
        }

        /// <summary>
        /// Veritabanının mevcut durumu hakkında bilgi verir
        /// </summary>
        public async Task<DatabaseInfo> GetDatabaseInfoAsync()
        {
            try
            {
                var canConnect = await CanConnectAsync();
                var appliedMigrations = canConnect ? await _context.Database.GetAppliedMigrationsAsync() : new List<string>();
                var pendingMigrations = canConnect ? await _context.Database.GetPendingMigrationsAsync() : new List<string>();

                return new DatabaseInfo
                {
                    CanConnect = canConnect,
                    AppliedMigrations = appliedMigrations.ToList(),
                    PendingMigrations = pendingMigrations.ToList(),
                    DatabaseExists = canConnect
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bilgisi alınırken hata oluştu");
                return new DatabaseInfo
                {
                    CanConnect = false,
                    DatabaseExists = false,
                    AppliedMigrations = new List<string>(),
                    PendingMigrations = new List<string>()
                };
            }
        }

        /// <summary>
        /// Development ortamında veritabanını sıfırlar ve yeniden oluşturur
        /// UYARI: Tüm veri silinir!
        /// </summary>
        public async Task RecreateDatabase()
        {
            try
            {
                _logger.LogWarning("Veritabanı yeniden oluşturuluyor - TÜM VERİ SİLİNECEK!");

                await _context.Database.EnsureDeletedAsync();
                _logger.LogInformation("Mevcut veritabanı silindi.");

                await _context.Database.MigrateAsync();
                _logger.LogInformation("Veritabanı yeniden oluşturuldu.");

                // Seed data yükle
                await DbInitializer.InitializeAsync(_context, isDevelopment: true);
                _logger.LogInformation("Örnek veriler yüklendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı yeniden oluşturulurken hata oluştu");
                throw;
            }
        }
    }

    /// <summary>
    /// Veritabanı durum bilgileri
    /// </summary>
    public class DatabaseInfo
    {
        public bool CanConnect { get; set; }
        public bool DatabaseExists { get; set; }
        public List<string> AppliedMigrations { get; set; } = new();
        public List<string> PendingMigrations { get; set; } = new();
    }
}
