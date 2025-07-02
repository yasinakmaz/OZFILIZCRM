using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CRM
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Syncfusion lisans konfigürasyonu
            // Not: Gerçek lisans anahtarınızı buraya eklemeniz gerekir
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY_HERE");

            // Syncfusion Blazor servislerini ekle
            builder.Services.AddSyncfusionBlazor();

            // Logging konfigürasyonu - Serilog ile dosya tabanlı loglama
            ConfigureLogging(builder);

            // Veritabanı konfigürasyonu
            ConfigureDatabase(builder);

            // Repository pattern servislerini kaydet
            ConfigureRepositories(builder);

            // Business logic servislerini kaydet
            ConfigureServices(builder);

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
     /// <summary>
    /// Logging sistemini konfigüre eder
    /// Serilog kullanarak dosya tabanlı ve konsol loglama yapar
    /// </summary>
    private static void ConfigureLogging(MauiAppBuilder builder)
        {
            // Serilog konfigürasyonu
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(FileSystem.AppDataDirectory, "logs", "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            // Serilog'u Microsoft.Extensions.Logging'e entegre et
            builder.Logging.AddSerilog(Log.Logger);
        }

        /// <summary>
        /// Entity Framework ve veritabanı bağlantısını konfigüre eder
        /// MSSQL Server kullanarak güvenli bağlantı sağlar
        /// </summary>
        private static void ConfigureDatabase(MauiAppBuilder builder)
        {
            // Connection string - gerçek projelerde appsettings.json'dan okunmalı
            // Bu örnekte basitlik için hardcode edildi
            var connectionString = "Server=localhost;Database=TeknikServisDB;Trusted_Connection=true;TrustServerCertificate=true;";

            // Geliştirme ortamında LocalDB kullanmak isterseniz:
            // var connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=TeknikServisDB;Trusted_Connection=true;";

            builder.Services.AddDbContext<TeknikServisDbContext>(options =>
            {
                options.UseSqlServer(connectionString);

                // Development ortamında sensitive data logging açılabilir
#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
            });
        }

        /// <summary>
        /// Repository pattern'ı kullanarak data access layer'ı konfigüre eder
        /// Her entity için özel repository servisi kaydeder
        /// </summary>
        private static void ConfigureRepositories(MauiAppBuilder builder)
        {
            // Generic repository
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Specialized repositories - her entity için özel repository
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
        }

        /// <summary>
        /// Business logic servislerini DI container'a kaydeder
        /// Her servis kendi sorumluluğuna uygun olarak organize edilmiştir
        /// </summary>
        private static void ConfigureServices(MauiAppBuilder builder)
        {
            // Authentication ve Authorization
            builder.Services.AddScoped<AuthService>();

            builder.Services.AddDatabaseMigration();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback connection string
                connectionString = "Server=localhost;Database=TeknikServisDB;Trusted_Connection=true;TrustServerCertificate=true;";
            }

            // EF Core DbContext konfigürasyonu
            builder.Services.AddDbContext<TeknikServisDbContext>(options =>
            {
                options.UseSqlServer(connectionString);

                // Development ortamında detaylı loglama
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                }
            });

            // Business services
            builder.Services.AddScoped<CustomerService>();
            builder.Services.AddScoped<ServiceManagementService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<DashboardService>();

            // Utility services
            builder.Services.AddScoped<LoggingService>();
            builder.Services.AddScoped<PdfService>();
            builder.Services.AddScoped<PrintService>();

            // Platform-specific services
            builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
            builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

            // HTTP Client - PDF export veya external API'ler için gerekebilir
            builder.Services.AddHttpClient();
        }
    }
