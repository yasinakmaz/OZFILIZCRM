using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using CRM.Data;
using CRM.Data.Repositories;
using CRM.Services;
using CRM.Data.Migrations;
using Syncfusion.Blazor;

namespace CRM
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // MAUI ve Font konfigürasyonu
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                });

            // Blazor WebView ekleme
            builder.Services.AddMauiBlazorWebView();

            // **KRİTİK**: Configuration dosyalarını yükle
            ConfigureAppSettings(builder);

            // **KRİTİK**: Logging sistemini konfigüre et
            ConfigureLogging(builder);

            // **KRİTİK**: Syncfusion lisansını konfigüre et
            ConfigureSyncfusion(builder);

            // **KRİTİK**: Veritabanı konfigürasyonu
            ConfigureDatabase(builder);

            // **KRİTİK**: Repository pattern servislerini kaydet
            ConfigureRepositories(builder);

            // **KRİTİK**: Business logic servislerini kaydet
            ConfigureServices(builder);

            // **KRİTİK**: Platform-specific servisler
            ConfigurePlatformServices(builder);

            // **KRİTİK**: Cross-cutting concerns
            ConfigureCrossCuttingConcerns(builder);

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
            // Development ortamında detaylı hata raporlama
            builder.Services.AddDeveloperExceptionPage();
#endif

            var app = builder.Build();

            // **KRİTİK**: Uygulama başlatıldığında database migration'ları çalıştır
            _ = Task.Run(async () => await InitializeDatabaseAsync(app));

            return app;
        }

        /// <summary>
        /// Configuration dosyalarını yükler (appsettings.json vs.)
        /// Platform-specific ayarları destekler
        /// </summary>
        private static void ConfigureAppSettings(MauiAppBuilder builder)
        {
            // Ana configuration dosyasını yükle
            var assembly = typeof(App).Assembly;
            using var stream = assembly.GetManifestResourceStream("CRM.appsettings.json");
            if (stream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
                builder.Configuration.AddConfiguration(config);
            }

            // Development environment için özel ayarlar
#if DEBUG
            using var devStream = assembly.GetManifestResourceStream("CRM.appsettings.Development.json");
            if (devStream != null)
            {
                var devConfig = new ConfigurationBuilder()
                    .AddJsonStream(devStream)
                    .Build();
                builder.Configuration.AddConfiguration(devConfig);
            }
#endif

            // Platform-specific secrets için
            builder.Configuration.AddUserSecrets<App>();
        }

        /// <summary>
        /// Serilog ile comprehensive logging sistemini konfigüre eder
        /// Hem dosya hem konsol logging yapar, error tracking dahil
        /// </summary>
        private static void ConfigureLogging(MauiAppBuilder builder)
        {
            // Log dosyalarının kaydedileceği dizin
            var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
            Directory.CreateDirectory(logDirectory);

            // Serilog konfigürasyonu - Production ready
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "TeknikServisCRM")
                .Enrich.WithProperty("Version", "1.0.0")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "error-.log"),
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    shared: true)
                .CreateLogger();

            // Microsoft.Extensions.Logging'e entegre et
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: true);
        }

        /// <summary>
        /// Syncfusion komponetlerini konfigüre eder
        /// License key management dahil
        /// </summary>
        private static void ConfigureSyncfusion(MauiAppBuilder builder)
        {
            // **UYARI**: Gerçek projede license key'i appsettings.json'dan alın
            var licenseKey = builder.Configuration["Syncfusion:LicenseKey"];

            if (!string.IsNullOrEmpty(licenseKey))
            {
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }
            else
            {
                // Development için community license kullanılabilir
                // Üretimde mutlaka geçerli license key olmalı
#if DEBUG
                Console.WriteLine("⚠️  UYARI: Syncfusion license key bulunamadı. Development modunda community edition kullanılıyor.");
#endif
            }

            // Syncfusion Blazor servislerini ekle
            builder.Services.AddSyncfusionBlazor(options =>
            {
                // Global theme ayarları
                options.IgnoreScriptIsolation = false;
            });
        }

        /// <summary>
        /// Entity Framework ve veritabanı bağlantısını konfigüre eder
        /// Connection string management, migration ve seeding dahil
        /// </summary>
        private static void ConfigureDatabase(MauiAppBuilder builder)
        {
            // Connection string'i appsettings.json'dan al
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Fallback connection strings - platform bazlı
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = DeviceInfo.Platform switch
                {
                    DevicePlatform.WinUI => builder.Configuration.GetConnectionString("LocalDB") ??
                                          "Server=(localdb)\\MSSQLLocalDB;Database=TeknikServisDB;Trusted_Connection=true;MultipleActiveResultSets=true;",
                    DevicePlatform.Android => Path.Combine(FileSystem.AppDataDirectory, "teknikservis.db"),
                    DevicePlatform.iOS => Path.Combine(FileSystem.AppDataDirectory, "teknikservis.db"),
                    DevicePlatform.macOS => Path.Combine(FileSystem.AppDataDirectory, "teknikservis.db"),
                    _ => "Server=localhost;Database=TeknikServisDB;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true;"
                };
            }

            // DbContext konfigürasyonu - platform bazlı
            builder.Services.AddDbContext<TeknikServisDbContext>((serviceProvider, options) =>
            {
                var logger = serviceProvider.GetService<ILogger<TeknikServisDbContext>>();

                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    // Windows'ta SQL Server kullan
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
                }
                else
                {
                    // Mobil platformlarda SQLite kullan
                    options.UseSqlite(connectionString, sqliteOptions =>
                    {
                        sqliteOptions.CommandTimeout(30);
                    });
                }

                // Development ortamında detaylı loglama
#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.LogTo(message => logger?.LogInformation(message), LogLevel.Information);
#endif
            });

            // Migration servisi
            builder.Services.AddDatabaseMigration();
        }

        /// <summary>
        /// Repository pattern'ı kullanarak data access layer'ı konfigüre eder
        /// Her entity için özel repository servisi kaydeder
        /// </summary>
        private static void ConfigureRepositories(MauiAppBuilder builder)
        {
            // Generic repository - tüm CRUD operasyonları için
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Specialized repositories - entity-specific business logic için
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
            builder.Services.AddScoped<IServiceTaskRepository, ServiceTaskRepository>();
            builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
        }

        /// <summary>
        /// Business logic servislerini DI container'a kaydeder
        /// Her servis kendi sorumluluğuna uygun olarak organize edilmiştir
        /// </summary>
        private static void ConfigureServices(MauiAppBuilder builder)
        {
            // **KRİTİK**: Authentication ve Authorization servisleri
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAuthenticationStateProvider, CustomAuthenticationStateProvider>();

            // **KRİTİK**: Core business servisleri
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<IServiceManagementService, ServiceManagementService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            // **KRİTİK**: Utility servisleri
            builder.Services.AddScoped<ILoggingService, LoggingService>();
            builder.Services.AddScoped<IPdfService, PdfService>();
            builder.Services.AddScoped<IPrintService, PrintService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IReportService, ReportService>();

            // **KRİTİK**: Cache servisi - performance için
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<ICacheService, MemoryCacheService>();

            // **KRİTİK**: Background servisleri
            builder.Services.AddSingleton<IBackgroundTaskService, BackgroundTaskService>();
        }

        /// <summary>
        /// Platform-specific servisleri konfigüre eder
        /// Her platform için optimize edilmiş servisler
        /// </summary>
        private static void ConfigurePlatformServices(MauiAppBuilder builder)
        {
            // **KRİTİK**: Platform servisleri
            builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
            builder.Services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            builder.Services.AddSingleton<IPreferences>(Preferences.Default);
            builder.Services.AddSingleton<IFilePicker>(FilePicker.Default);
            builder.Services.AddSingleton<IShare>(Share.Default);

            // HTTP Client - external API'ler ve dosya indirme için
            builder.Services.AddHttpClient("TeknikServisAPI", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "TeknikServisCRM/1.0");
            });

            // Platform-specific implementations
            RegisterPlatformSpecificServices(builder);
        }

        /// <summary>
        /// Cross-cutting concerns'i konfigüre eder
        /// Error handling, validation, caching vb.
        /// </summary>
        private static void ConfigureCrossCuttingConcerns(MauiAppBuilder builder)
        {
            // **KRİTİK**: Global error handler
            builder.Services.AddScoped<IGlobalErrorHandler, GlobalErrorHandler>();

            // **KRİTİK**: Validation servisleri
            builder.Services.AddScoped<IValidationService, ValidationService>();

            // **KRİTİK**: Performance monitoring
            builder.Services.AddScoped<IPerformanceMonitor, PerformanceMonitor>();

            // **KRİTİK**: Security servisleri
            builder.Services.AddScoped<ISecurityService, SecurityService>();
            builder.Services.AddScoped<IEncryptionService, EncryptionService>();
        }

        /// <summary>
        /// Platform-specific servis implementasyonlarını kaydeder
        /// </summary>
        private static void RegisterPlatformSpecificServices(MauiAppBuilder builder)
        {
#if WINDOWS
            builder.Services.AddScoped<IPlatformSpecificService, WindowsSpecificService>();
#elif ANDROID
            builder.Services.AddScoped<IPlatformSpecificService, AndroidSpecificService>();
#elif IOS
            builder.Services.AddScoped<IPlatformSpecificService, iOSSpecificService>();
#elif MACCATALYST
            builder.Services.AddScoped<IPlatformSpecificService, MacCatalystSpecificService>();
#else
            builder.Services.AddScoped<IPlatformSpecificService, DefaultPlatformSpecificService>();
#endif
        }

        /// <summary>
        /// Uygulama başladığında database migration'larını çalıştırır
        /// Seed data'yı yükler ve initial setup yapar
        /// </summary>
        private static async Task InitializeDatabaseAsync(MauiApp app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<MauiApp>>();

                logger.LogInformation("🚀 Uygulama başlatılıyor, veritabanı kontrol ediliyor...");

                // Migration servisi ile database'i güncelle
                var migrationService = services.GetRequiredService<DatabaseMigrationService>();
                await migrationService.ApplyMigrationsAsync();

                // Seed data yükle
                var context = services.GetRequiredService<TeknikServisDbContext>();
                await DbInitializer.InitializeAsync(context, isDevelopment: true);

                logger.LogInformation("✅ Veritabanı başarıyla hazırlandı.");
            }
            catch (Exception ex)
            {
                // Kritik hata - uygulama çalışamaz
                Log.Fatal(ex, "💥 FATAL: Veritabanı başlatılamadı, uygulama sonlandırılıyor");

                // Kullanıcıya hata göster ve uygulamayı güvenli şekilde kapat
                await Application.Current.MainPage.DisplayAlert(
                    "Kritik Hata",
                    "Veritabanı bağlantısı kurulamadı. Lütfen teknik destek ile iletişime geçin.",
                    "Tamam");

                Application.Current.Quit();
            }
        }
    }
}