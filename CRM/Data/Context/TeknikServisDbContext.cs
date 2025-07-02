using System.Reflection.Emit;

namespace CRM.Data.Context
{
    /// <summary>
    /// Entity Framework Core DbContext sınıfı
    /// Veritabanı bağlantısı ve entity konfigürasyonlarını yönetir
    /// </summary>
    public class TeknikServisDbContext : DbContext
    {
        /// <summary>
        /// Constructor - DI container'dan DbContextOptions alır
        /// </summary>
        public TeknikServisDbContext(DbContextOptions<TeknikServisDbContext> options) : base(options)
        {
        }

        // DbSet tanımlamaları - Her entity için tablo erişimi sağlar

        /// <summary>
        /// Kullanıcılar tablosu
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Müşteriler tablosu
        /// </summary>
        public DbSet<Customer> Customers { get; set; }

        /// <summary>
        /// Servisler tablosu
        /// </summary>
        public DbSet<Service> Services { get; set; }

        /// <summary>
        /// Servis görevleri tablosu
        /// </summary>
        public DbSet<ServiceTask> ServiceTasks { get; set; }

        /// <summary>
        /// Servis-Kullanıcı ilişki tablosu
        /// </summary>
        public DbSet<ServiceUser> ServiceUsers { get; set; }

        /// <summary>
        /// Audit log tablosu
        /// </summary>
        public DbSet<AuditLog> AuditLogs { get; set; }

        /// <summary>
        /// Model konfigürasyonları
        /// Entity ilişkileri, constraints ve indexler burada tanımlanır
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ServiceUser composite primary key tanımı
            modelBuilder.Entity<ServiceUser>()
                .HasKey(su => new { su.ServiceId, su.UserId });

            // Enum to int conversion konfigürasyonları
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<int>();

            modelBuilder.Entity<Customer>()
                .Property(c => c.CompanyType)
                .HasConversion<int>();

            modelBuilder.Entity<Service>()
                .Property(s => s.Status)
                .HasConversion<int>();

            modelBuilder.Entity<ServiceTask>()
                .Property(st => st.Priority)
                .HasConversion<int>();

            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.TaxNumber)
                .IsUnique();

            // Foreign key relationships konfigürasyonu

            // Service -> Customer ilişkisi
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Müşteri silinirse servisler silinmez

            // Service -> User (Creator) ilişkisi
            modelBuilder.Entity<Service>()
                .HasOne(s => s.CreatedByUser)
                .WithMany(u => u.CreatedServices)
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Service -> User (Approver) ilişkisi
            modelBuilder.Entity<Service>()
                .HasOne(s => s.ApprovedByUser)
                .WithMany()
                .HasForeignKey(s => s.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ServiceTask -> Service ilişkisi
            modelBuilder.Entity<ServiceTask>()
                .HasOne(st => st.Service)
                .WithMany(s => s.ServiceTasks)
                .HasForeignKey(st => st.ServiceId)
                .OnDelete(DeleteBehavior.Cascade); // Servis silinirse task'lar da silinir

            // ServiceTask -> User (Completer) ilişkisi
            modelBuilder.Entity<ServiceTask>()
                .HasOne(st => st.CompletedByUser)
                .WithMany()
                .HasForeignKey(st => st.CompletedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ServiceUser ilişkileri
            modelBuilder.Entity<ServiceUser>()
                .HasOne(su => su.Service)
                .WithMany(s => s.ServiceUsers)
                .HasForeignKey(su => su.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServiceUser>()
                .HasOne(su => su.User)
                .WithMany(u => u.ServiceUsers)
                .HasForeignKey(su => su.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceUser>()
                .HasOne(su => su.AssignedByUser)
                .WithMany()
                .HasForeignKey(su => su.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // AuditLog -> User ilişkisi
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Default values ve constraints
            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Customer>()
                .Property(c => c.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<ServiceTask>()
                .Property(st => st.IsCompleted)
                .HasDefaultValue(false);

            modelBuilder.Entity<ServiceUser>()
                .Property(su => su.IsActive)
                .HasDefaultValue(true);

            // Performans için indexler
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.Status);

            modelBuilder.Entity<Service>()
                .HasIndex(s => s.ScheduledDateTime);

            modelBuilder.Entity<Service>()
                .HasIndex(s => s.CreatedDate);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Timestamp);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.UserId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => new { al.EntityType, al.EntityId });
        }
    }
}
