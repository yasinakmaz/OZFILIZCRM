using Microsoft.Extensions.Logging;

namespace CRM.Data.Migrations
{
    /// <summary>
    /// Migration işlemleri için extension metodları
    /// </summary>
    public static class MigrationExtensions
    {
        /// <summary>
        /// ServiceCollection'a migration servislerini ekler
        /// </summary>
        public static IServiceCollection AddDatabaseMigration(this IServiceCollection services)
        {
            services.AddScoped<DatabaseMigrationService>();
            return services;
        }

        /// <summary>
        /// Uygulama başlangıcında migration'ları çalıştırır
        /// </summary>
        public static async Task<IHost> MigrateDatabaseAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var context = services.GetRequiredService<TeknikServisDbContext>();
                var migrationService = services.GetRequiredService<DatabaseMigrationService>();
                var logger = services.GetRequiredService<ILogger<DatabaseMigrationService>>();

                logger.LogInformation("Veritabanı migration işlemi başlatılıyor...");

                // Migration'ları uygula
                await migrationService.ApplyMigrationsAsync();

                // Development ortamında seed data yükle
                var environment = services.GetRequiredService<IWebHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    await DbInitializer.InitializeAsync(context, isDevelopment: true);
                    logger.LogInformation("Development seed data yüklendi.");
                }
                else
                {
                    // Production'da sadece temel veriler
                    await DbInitializer.InitializeAsync(context, isDevelopment: false);
                    logger.LogInformation("Production temel veriler kontrol edildi.");
                }

                logger.LogInformation("Veritabanı migration işlemi tamamlandı.");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<DatabaseMigrationService>>();
                logger.LogError(ex, "Veritabanı migration sırasında hata oluştu");

                // Development ortamında hata fırlatılabilir
                // Production'da logging yeterli olabilir
                throw;
            }

            return host;
        }
    }
}
