// Data/TeknikServisDbContext.cs - Entity Framework DbContext
using CloudKit;
using CRM.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection.Emit;

namespace CRM.Data
{
    /// <summary>
    /// Ana veritabanı context'i - Entity Framework configuration ve DbSet'leri içerir
    /// Production-ready ayarlar ve performance optimizasyonları dahil
    /// </summary>
    public class TeknikServisDbContext : DbContext
    {
        private readonly ILogger<TeknikServisDbContext>? _logger;

        public TeknikServisDbContext(DbContextOptions<TeknikServisDbContext> options) : base(options)
        {
        }

        public TeknikServisDbContext(DbContextOptions<TeknikServisDbContext> options, ILogger<TeknikServisDbContext> logger)
            : base(options)
        {
            _logger = logger;
        }

        // **DBSET'LER - Veritabanı tabloları**
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<ServiceTask> ServiceTasks { get; set; } = null!;
        public DbSet<SystemLog> SystemLogs { get; set; } = null!;

        /// <summary>
        /// Entity configuration'ları burada tanımlanır
        /// Foreign key'ler, index'ler, constraints vb.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // **USER ENTITY CONFIGURATION**
            ConfigureUserEntity(modelBuilder);

            // **CUSTOMER ENTITY CONFIGURATION** 
            ConfigureCustomerEntity(modelBuilder);

            // **SERVICE ENTITY CONFIGURATION**
            ConfigureServiceEntity(modelBuilder);

            // **SERVICE TASK ENTITY CONFIGURATION**
            ConfigureServiceTaskEntity(modelBuilder);

            // **SYSTEM LOG ENTITY CONFIGURATION**
            ConfigureSystemLogEntity(modelBuilder);

            // **GLOBAL CONFIGURATIONS**
            ConfigureGlobalSettings(modelBuilder);
        }

        /// <summary>
        /// User entity için özel konfigürasyonlar
        /// Index'ler, constraints ve relationships
        /// </summary>
        private static void ConfigureUserEntity(ModelBuilder modelBuilder)
        {
            var userEntity = modelBuilder.Entity<User>();

            // **TABLO ADI**
            userEntity.ToTable("Users");

            // **PRIMARY KEY**
            userEntity.HasKey(u => u.Id);

            // **UNIQUE CONSTRAINTS**
            userEntity.HasIndex(u => u.Username)
                     .IsUnique()
                     .HasDatabaseName("IX_Users_Username");

            userEntity.HasIndex(u => u.Email)
                     .IsUnique()
                     .HasDatabaseName("IX_Users_Email");

            // **PERFORMANCE INDEX'LER**
            userEntity.HasIndex(u => u.Role)
                     .HasDatabaseName("IX_Users_Role");

            userEntity.HasIndex(u => u.IsActive)
                     .HasDatabaseName("IX_Users_IsActive");

            userEntity.HasIndex(u => u.LastLoginDate)
                     .HasDatabaseName("IX_Users_LastLoginDate");

            // **PROPERTY CONFIGURATIONS**
            userEntity.Property(u => u.Username)
                     .IsRequired()
                     .HasMaxLength(50);

            userEntity.Property(u => u.Email)
                     .IsRequired()
                     .HasMaxLength(100);

            userEntity.Property(u => u.PasswordHash)
                     .IsRequired()
                     .HasMaxLength(255); // Hash'ler için yeterli alan

            userEntity.Property(u => u.FirstName)
                     .IsRequired()
                     .HasMaxLength(50);

            userEntity.Property(u => u.LastName)
                     .IsRequired()
                     .HasMaxLength(50);

            userEntity.Property(u => u.PhoneNumber)
                     .HasMaxLength(15);

            userEntity.Property(u => u.Role)
                     .IsRequired()
                     .HasConversion<int>(); // Enum'u int olarak sakla

            userEntity.Property(u => u.ProfileImageBase64)
                     .HasMaxLength(1000000); // 1MB limit

            userEntity.Property(u => u.Notes)
                     .HasMaxLength(500);

            // **SOFT DELETE FILTER**
            userEntity.HasQueryFilter(u => !u.IsDeleted);

            // **RELATIONSHIPS**
            userEntity.HasMany(u => u.AssignedServices)
                     .WithOne(s => s.AssignedUser)
                     .HasForeignKey(s => s.AssignedUserId)
                     .OnDelete(DeleteBehavior.SetNull);

            userEntity.HasMany(u => u.CompletedTasks)
                     .WithOne(t => t.CompletedByUser)
                     .HasForeignKey(t => t.CompletedByUserId)
                     .OnDelete(DeleteBehavior.SetNull);
        }

        /// <summary>
        /// Customer entity için özel konfigürasyonlar
        /// Business-specific index'ler ve validations
        /// </summary>
        private static void ConfigureCustomerEntity(ModelBuilder modelBuilder)
        {
            var customerEntity = modelBuilder.Entity<Customer>();

            // **TABLO ADI**
            customerEntity.ToTable("Customers");

            // **PRIMARY KEY**
            customerEntity.HasKey(c => c.Id);

            // **UNIQUE CONSTRAINTS**
            customerEntity.HasIndex(c => c.TaxNumber)
                         .IsUnique()
                         .HasDatabaseName("IX_Customers_TaxNumber")
                         .HasFilter("TaxNumber IS NOT NULL"); // Null değerler için unique constraint çalışmasın

            // **SEARCH INDEX'LER**
            customerEntity.HasIndex(c => c.CompanyName)
                         .HasDatabaseName("IX_Customers_CompanyName");

            customerEntity.HasIndex(c => c.Email)
                         .HasDatabaseName("IX_Customers_Email");

            customerEntity.HasIndex(c => c.PhoneNumber)
                         .HasDatabaseName("IX_Customers_PhoneNumber");

            customerEntity.HasIndex(c => c.IsActive)
                         .HasDatabaseName("IX_Customers_IsActive");

            customerEntity.HasIndex(c => c.CustomerType)
                         .HasDatabaseName("IX_Customers_CustomerType");

            // **LOCATION INDEX - Composite index for address searches**
            customerEntity.HasIndex(c => new { c.City, c.District })
                         .HasDatabaseName("IX_Customers_Location");

            // **PROPERTY CONFIGURATIONS**
            customerEntity.Property(c => c.CompanyName)
                         .IsRequired()
                         .HasMaxLength(100);

            customerEntity.Property(c => c.TaxNumber)
                         .HasMaxLength(11);

            customerEntity.Property(c => c.ContactPerson)
                         .HasMaxLength(50);

            customerEntity.Property(c => c.Email)
                         .HasMaxLength(100);

            customerEntity.Property(c => c.PhoneNumber)
                         .HasMaxLength(15);

            customerEntity.Property(c => c.MobileNumber)
                         .HasMaxLength(15);

            customerEntity.Property(c => c.Address)
                         .HasMaxLength(500);

            customerEntity.Property(c => c.City)
                         .HasMaxLength(50);

            customerEntity.Property(c => c.District)
                         .HasMaxLength(50);

            customerEntity.Property(c => c.PostalCode)
                         .HasMaxLength(10);

            customerEntity.Property(c => c.CustomerType)
                         .IsRequired()
                         .HasConversion<int>();

            customerEntity.Property(c => c.Notes)
                         .HasMaxLength(1000);

            customerEntity.Property(c => c.CreditLimit)
                         .HasPrecision(18, 2); // Para birimi için decimal precision

            // **SOFT DELETE FILTER**
            customerEntity.HasQueryFilter(c => !c.IsDeleted);

            // **RELATIONSHIPS**
            customerEntity.HasMany(c => c.Services)
                         .WithOne(s => s.Customer)
                         .HasForeignKey(s => s.CustomerId)
                         .OnDelete(DeleteBehavior.Restrict); // Customer silinememeli eğer servisleri varsa
        }

        /// <summary>
        /// Service entity için özel konfigürasyonlar
        /// Business workflow ve performance optimizasyonları
        /// </summary>
        private static void ConfigureServiceEntity(ModelBuilder modelBuilder)
        {
            var serviceEntity = modelBuilder.Entity<Service>();

            // **TABLO ADI**
            serviceEntity.ToTable("Services");

            // **PRIMARY KEY**
            serviceEntity.HasKey(s => s.Id);

            // **PERFORMANCE INDEX'LER**
            serviceEntity.HasIndex(s => s.Status)
                        .HasDatabaseName("IX_Services_Status");

            serviceEntity.HasIndex(s => s.Priority)
                        .HasDatabaseName("IX_Services_Priority");

            serviceEntity.HasIndex(s => s.CustomerId)
                        .HasDatabaseName("IX_Services_CustomerId");

            serviceEntity.HasIndex(s => s.AssignedUserId)
                        .HasDatabaseName("IX_Services_AssignedUserId");

            serviceEntity.HasIndex(s => s.CreatedDate)
                        .HasDatabaseName("IX_Services_CreatedDate");

            serviceEntity.HasIndex(s => s.ExpectedCompletionDate)
                        .HasDatabaseName("IX_Services_ExpectedCompletionDate");

            // **COMPOSITE INDEX'LER - Multi-column searches için**
            serviceEntity.HasIndex(s => new { s.Status, s.Priority })
                        .HasDatabaseName("IX_Services_Status_Priority");

            serviceEntity.HasIndex(s => new { s.CustomerId, s.Status })
                        .HasDatabaseName("IX_Services_Customer_Status");

            serviceEntity.HasIndex(s => new { s.AssignedUserId, s.Status })
                        .HasDatabaseName("IX_Services_User_Status");

            // **PROPERTY CONFIGURATIONS**
            serviceEntity.Property(s => s.Title)
                        .IsRequired()
                        .HasMaxLength(200);

            serviceEntity.Property(s => s.Description)
                        .IsRequired()
                        .HasMaxLength(2000);

            serviceEntity.Property(s => s.Status)
                        .IsRequired()
                        .HasConversion<int>();

            serviceEntity.Property(s => s.Priority)
                        .IsRequired()
                        .HasConversion<int>();

            serviceEntity.Property(s => s.ServiceAmount)
                        .HasPrecision(18, 2);

            serviceEntity.Property(s => s.DeviceModel)
                        .HasMaxLength(100);

            serviceEntity.Property(s => s.DeviceSerialNumber)
                        .HasMaxLength(100);

            serviceEntity.Property(s => s.ProblemDescription)
                        .HasMaxLength(1000);

            serviceEntity.Property(s => s.SolutionDescription)
                        .HasMaxLength(1000);

            serviceEntity.Property(s => s.UsedParts)
                        .HasMaxLength(500);

            serviceEntity.Property(s => s.TechnicianNotes)
                        .HasMaxLength(1000);

            serviceEntity.Property(s => s.CustomerNotes)
                        .HasMaxLength(1000);

            // **SOFT DELETE FILTER**
            serviceEntity.HasQueryFilter(s => !s.IsDeleted);

            // **RELATIONSHIPS**
            serviceEntity.HasOne(s => s.Customer)
                        .WithMany(c => c.Services)
                        .HasForeignKey(s => s.CustomerId)
                        .OnDelete(DeleteBehavior.Restrict);

            serviceEntity.HasOne(s => s.AssignedUser)
                        .WithMany(u => u.AssignedServices)
                        .HasForeignKey(s => s.AssignedUserId)
                        .OnDelete(DeleteBehavior.SetNull);

            serviceEntity.HasMany(s => s.ServiceTasks)
                        .WithOne(t => t.Service)
                        .HasForeignKey(t => t.ServiceId)
                        .OnDelete(DeleteBehavior.Cascade); // Service silinince task'lar da silinsin
        }

        /// <summary>
        /// ServiceTask entity için özel konfigürasyonlar
        /// Task management ve progress tracking optimizasyonları
        /// </summary>
        private static void ConfigureServiceTaskEntity(ModelBuilder modelBuilder)
        {
            var taskEntity = modelBuilder.Entity<ServiceTask>();

            // **TABLO ADI**
            taskEntity.ToTable("ServiceTasks");

            // **PRIMARY KEY**
            taskEntity.HasKey(t => t.Id);

            // **PERFORMANCE INDEX'LER**
            taskEntity.HasIndex(t => t.ServiceId)
                     .HasDatabaseName("IX_ServiceTasks_ServiceId");

            taskEntity.HasIndex(t => t.IsCompleted)
                     .HasDatabaseName("IX_ServiceTasks_IsCompleted");

            taskEntity.HasIndex(t => t.Priority)
                     .HasDatabaseName("IX_ServiceTasks_Priority");

            taskEntity.HasIndex(t => t.CompletedByUserId)
                     .HasDatabaseName("IX_ServiceTasks_CompletedByUserId");

            taskEntity.HasIndex(t => t.CompletedDate)
                     .HasDatabaseName("IX_ServiceTasks_CompletedDate");

            // **COMPOSITE INDEX'LER**
            taskEntity.HasIndex(t => new { t.ServiceId, t.IsCompleted })
                     .HasDatabaseName("IX_ServiceTasks_Service_Completed");

            // **PROPERTY CONFIGURATIONS**
            taskEntity.Property(t => t.Description)
                     .IsRequired()
                     .HasMaxLength(500);

            taskEntity.Property(t => t.Priority)
                     .IsRequired()
                     .HasConversion<int>();

            taskEntity.Property(t => t.Notes)
                     .HasMaxLength(1000);

            // **SOFT DELETE FILTER**
            taskEntity.HasQueryFilter(t => !t.IsDeleted);

            // **RELATIONSHIPS**
            taskEntity.HasOne(t => t.Service)
                     .WithMany(s => s.ServiceTasks)
                     .HasForeignKey(t => t.ServiceId)
                     .OnDelete(DeleteBehavior.Cascade);

            taskEntity.HasOne(t => t.CompletedByUser)
                     .WithMany(u => u.CompletedTasks)
                     .HasForeignKey(t => t.CompletedByUserId)
                     .OnDelete(DeleteBehavior.SetNull);
        }

        /// <summary>
        /// SystemLog entity için özel konfigürasyonlar
        /// Logging ve audit trail optimizasyonları
        /// </summary>
        private static void ConfigureSystemLogEntity(ModelBuilder modelBuilder)
        {
            var logEntity = modelBuilder.Entity<SystemLog>();

            // **TABLO ADI**
            logEntity.ToTable("SystemLogs");

            // **PRIMARY KEY**
            logEntity.HasKey(l => l.Id);

            // **PERFORMANCE INDEX'LER - Log search optimizasyonu için kritik**
            logEntity.HasIndex(l => l.Action)
                    .HasDatabaseName("IX_SystemLogs_Action");

            logEntity.HasIndex(l => l.EntityType)
                    .HasDatabaseName("IX_SystemLogs_EntityType");

            logEntity.HasIndex(l => l.EntityId)
                    .HasDatabaseName("IX_SystemLogs_EntityId");

            logEntity.HasIndex(l => l.UserId)
                    .HasDatabaseName("IX_SystemLogs_UserId");

            logEntity.HasIndex(l => l.CreatedDate)
                    .HasDatabaseName("IX_SystemLogs_CreatedDate");

            logEntity.HasIndex(l => l.LogLevel)
                    .HasDatabaseName("IX_SystemLogs_LogLevel");

            // **COMPOSITE INDEX'LER - Complex search queries için**
            logEntity.HasIndex(l => new { l.EntityType, l.EntityId })
                    .HasDatabaseName("IX_SystemLogs_Entity");

            logEntity.HasIndex(l => new { l.UserId, l.CreatedDate })
                    .HasDatabaseName("IX_SystemLogs_User_Date");

            logEntity.HasIndex(l => new { l.LogLevel, l.CreatedDate })
                    .HasDatabaseName("IX_SystemLogs_Level_Date");

            // **PROPERTY CONFIGURATIONS**
            logEntity.Property(l => l.Action)
                    .IsRequired()
                    .HasMaxLength(100);

            logEntity.Property(l => l.EntityType)
                    .IsRequired()
                    .HasMaxLength(50);

            logEntity.Property(l => l.Description)
                    .HasMaxLength(2000);

            logEntity.Property(l => l.IpAddress)
                    .HasMaxLength(45); // IPv6 için yeterli

            logEntity.Property(l => l.UserAgent)
                    .HasMaxLength(500);

            logEntity.Property(l => l.LogLevel)
                    .IsRequired()
                    .HasConversion<int>();

            logEntity.Property(l => l.ExceptionDetails)
                    .HasMaxLength(4000);

            logEntity.Property(l => l.AdditionalData)
                    .HasMaxLength(4000);

            // **NO SOFT DELETE FOR LOGS** - Log'lar asla silinmemeli
            // Log entity'sinde IsDeleted field'ı yok, bu kasıtlı

            // **RELATIONSHIPS**
            logEntity.HasOne(l => l.User)
                    .WithMany(u => u.SystemLogs)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.SetNull); // User silinince log'lar kalsın
        }

        /// <summary>
        /// Global ayarlar ve cross-cutting configurations
        /// </summary>
        private static void ConfigureGlobalSettings(ModelBuilder modelBuilder)
        {
            // **GLOBAL DATETIME CONFIGURATION**
            // Tüm DateTime alanları için precision ayarı
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("datetime2"); // SQL Server için precision
                    }
                }
            }

            // **GLOBAL DECIMAL CONFIGURATION**
            // Para birimi alanları için varsayılan precision
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    {
                        if (property.GetPrecision() == null)
                        {
                            property.SetPrecision(18);
                            property.SetScale(2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// DbContext save operations'ında automatic timestamp güncellemesi
        /// Bu method her veri değişikliğinde otomatik çalışır
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // **AUTOMATIC TIMESTAMP UPDATES**
            UpdateTimestamps();

            // **AUDIT LOGGING** - İsteğe bağlı olarak açılabilir
#if DEBUG
            LogEntityChanges();
#endif

            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
#if DEBUG
            LogEntityChanges();
#endif
            return base.SaveChanges();
        }

        /// <summary>
        /// Otomatik timestamp güncellemesi
        /// BaseEntity'den türeyen tüm entity'ler için çalışır
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.Now;
                        entry.Entity.UpdatedDate = null;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.Now;
                        // CreatedDate'i korumak için readonly yap
                        entry.Property(e => e.CreatedDate).IsModified = false;
                        break;

                    case EntityState.Deleted:
                        // Soft delete implementation
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedDate = DateTime.Now;
                        entry.Entity.UpdatedDate = DateTime.Now;
                        break;
                }
            }
        }

        /// <summary>
        /// Development modunda entity değişikliklerini loglar
        /// Debugging ve audit için kullanılır
        /// </summary>
        private void LogEntityChanges()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                _logger?.LogInformation(
                    "Entity {EntityType} with ID {EntityId} was {State}",
                    entry.Entity.GetType().Name,
                    entry.Property("Id")?.CurrentValue,
                    entry.State);
            }
        }

        /// <summary>
        /// Development modunda database'i temizle ve yeniden oluştur
        /// SADECE DEVELOPMENT ORTAMINDA KULLAN!
        /// </summary>
        public async Task RecreateDatabase()
        {
#if DEBUG
            _logger?.LogWarning("⚠️ Veritabanı yeniden oluşturuluyor - TÜM VERİLER SİLİNECEK!");

            await Database.EnsureDeletedAsync();
            await Database.EnsureCreatedAsync();

            _logger?.LogInformation("✅ Veritabanı başarıyla yeniden oluşturuldu.");
#else
            throw new InvalidOperationException("RecreateDatabase sadece development modunda kullanılabilir!");
#endif
        }
    }
}