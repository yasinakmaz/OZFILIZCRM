using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.Data.Migrations
{
    /// <summary>
    /// Veritabanı başlatma ve seed data oluşturma sınıfı
    /// İlk çalıştırmada gerekli tabloları ve örnek verileri oluşturur
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Veritabanını başlatır ve seed data'yı yükler
        /// Production ortamında dikkatli kullanılmalıdır
        /// </summary>
        public static async Task InitializeAsync(TeknikServisDbContext context, bool isDevelopment = false)
        {
            try
            {
                // Veritabanını oluştur (yoksa)
                await context.Database.EnsureCreatedAsync();

                // Migration'ları uygula
                if (context.Database.GetPendingMigrations().Any())
                {
                    await context.Database.MigrateAsync();
                }

                // Seed data yükle
                await SeedDataAsync(context, isDevelopment);
            }
            catch (Exception ex)
            {
                // Hata loglama burada yapılabilir
                Console.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Örnek verileri yükler
        /// </summary>
        private static async Task SeedDataAsync(TeknikServisDbContext context, bool isDevelopment)
        {
            // Admin kullanıcısını kontrol et ve oluştur
            await SeedAdminUserAsync(context);

            // Development ortamında örnek veriler
            if (isDevelopment)
            {
                await SeedDevelopmentDataAsync(context);
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Varsayılan admin kullanıcısını oluşturur
        /// Email: admin@teknikservis.com
        /// Password: Admin123!
        /// </summary>
        private static async Task SeedAdminUserAsync(TeknikServisDbContext context)
        {
            const string adminEmail = "admin@teknikservis.com";

            var existingAdmin = await context.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new User
                {
                    Username = "admin",
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                await context.Users.AddAsync(adminUser);

                // İlk log kaydı
                var initialLog = new AuditLog
                {
                    UserId = 1, // Admin user ID (otomatik olarak 1 olacak)
                    Action = "SYSTEM_INIT",
                    EntityType = "System",
                    Description = "Sistem başlatıldı ve admin kullanıcısı oluşturuldu",
                    Timestamp = DateTime.Now
                };

                await context.AuditLogs.AddAsync(initialLog);
            }
        }

        /// <summary>
        /// Development ortamı için örnek veriler
        /// </summary>
        private static async Task SeedDevelopmentDataAsync(TeknikServisDbContext context)
        {
            // Örnek kullanıcılar oluştur
            await SeedSampleUsersAsync(context);

            // Örnek müşteriler oluştur
            await SeedSampleCustomersAsync(context);

            // Örnek servisler oluştur
            await SeedSampleServicesAsync(context);
        }

        /// <summary>
        /// Örnek kullanıcılar oluşturur
        /// </summary>
        private static async Task SeedSampleUsersAsync(TeknikServisDbContext context)
        {
            var sampleUsers = new[]
            {
                new User
                {
                    Username = "supervisor1",
                    Email = "supervisor@teknikservis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                    Role = UserRole.Supervisor,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    Username = "teknisyen1",
                    Email = "teknisyen1@teknikservis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                    Role = UserRole.Technician,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new User
                {
                    Username = "teknisyen2",
                    Email = "teknisyen2@teknikservis.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                    Role = UserRole.Technician,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            };

            foreach (var user in sampleUsers)
            {
                var existing = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (existing == null)
                {
                    await context.Users.AddAsync(user);
                }
            }
        }

        /// <summary>
        /// Örnek müşteriler oluşturur
        /// </summary>
        private static async Task SeedSampleCustomersAsync(TeknikServisDbContext context)
        {
            var sampleCustomers = new[]
            {
                new Customer
                {
                    CompanyName = "Acme Teknoloji A.Ş.",
                    CompanyType = CompanyType.Corporate,
                    TaxNumber = "1234567890",
                    TaxOffice = "Kadıköy Vergi Dairesi",
                    AuthorizedPersonName = "Ahmet Yılmaz",
                    PhoneNumber = "0216 123 45 67",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Customer
                {
                    CompanyName = "Mehmet Özkan",
                    CompanyType = CompanyType.Individual,
                    TaxNumber = "12345678901",
                    AuthorizedPersonName = "Mehmet Özkan",
                    PhoneNumber = "0532 123 45 67",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Customer
                {
                    CompanyName = "Beta Yazılım Ltd. Şti.",
                    CompanyType = CompanyType.Corporate,
                    TaxNumber = "9876543210",
                    TaxOffice = "Beşiktaş Vergi Dairesi",
                    AuthorizedPersonName = "Ayşe Demir",
                    PhoneNumber = "0212 987 65 43",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Customer
                {
                    CompanyName = "Fatma Kaya",
                    CompanyType = CompanyType.Individual,
                    TaxNumber = "10987654321",
                    AuthorizedPersonName = "Fatma Kaya",
                    PhoneNumber = "0545 987 65 43",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            };

            foreach (var customer in sampleCustomers)
            {
                var existing = await context.Customers
                    .FirstOrDefaultAsync(c => c.TaxNumber == customer.TaxNumber);

                if (existing == null)
                {
                    await context.Customers.AddAsync(customer);
                }
            }
        }

        /// <summary>
        /// Örnek servisler oluşturur
        /// </summary>
        private static async Task SeedSampleServicesAsync(TeknikServisDbContext context)
        {
            // Önce kaydetilen müşterileri ve kullanıcıları alalım
            await context.SaveChangesAsync();

            var customers = await context.Customers.Take(2).ToListAsync();
            var users = await context.Users.Where(u => u.Role != UserRole.Admin).Take(2).ToListAsync();

            if (!customers.Any() || !users.Any()) return;

            var sampleServices = new[]
            {
                new Service
                {
                    CustomerId = customers[0].Id,
                    CreatedByUserId = 1, // Admin
                    Status = ServiceStatus.Pending,
                    ServiceAmount = 1500.00m,
                    ScheduledDateTime = DateTime.Now.AddDays(1),
                    Notes = "Bilgisayar performans sorunu, yavaş çalışıyor",
                    CreatedDate = DateTime.Now.AddDays(-2)
                },
                new Service
                {
                    CustomerId = customers[1].Id,
                    CreatedByUserId = 1, // Admin
                    Status = ServiceStatus.InProgress,
                    ServiceAmount = 800.00m,
                    ScheduledDateTime = DateTime.Now.AddHours(-2),
                    ServiceStartDateTime = DateTime.Now.AddHours(-1),
                    Notes = "Yazıcı bağlantı sorunu",
                    CreatedDate = DateTime.Now.AddDays(-1)
                }
            };

            foreach (var service in sampleServices)
            {
                await context.Services.AddAsync(service);
            }

            await context.SaveChangesAsync();

            // Servis görevleri ekle
            var services = await context.Services.ToListAsync();

            if (services.Any())
            {
                var sampleTasks = new[]
                {
                    new ServiceTask
                    {
                        ServiceId = services[0].Id,
                        Description = "Sistem performans analizi yapılacak",
                        Priority = Priority.High,
                        IsCompleted = false,
                        CreatedDate = DateTime.Now.AddDays(-2)
                    },
                    new ServiceTask
                    {
                        ServiceId = services[0].Id,
                        Description = "Gereksiz dosyalar temizlenecek",
                        Priority = Priority.Normal,
                        IsCompleted = false,
                        CreatedDate = DateTime.Now.AddDays(-2)
                    },
                    new ServiceTask
                    {
                        ServiceId = services[1].Id,
                        Description = "Yazıcı sürücüleri güncellenecek",
                        Priority = Priority.High,
                        IsCompleted = true,
                        CompletedDate = DateTime.Now.AddHours(-1),
                        CompletedByUserId = users[0].Id,
                        CreatedDate = DateTime.Now.AddDays(-1)
                    }
                };

                foreach (var task in sampleTasks)
                {
                    await context.ServiceTasks.AddAsync(task);
                }

                // Kullanıcı atamaları
                var sampleAssignments = new[]
                {
                    new ServiceUser
                    {
                        ServiceId = services[1].Id,
                        UserId = users[0].Id,
                        AssignedByUserId = 1, // Admin
                        AssignedDate = DateTime.Now.AddDays(-1),
                        IsActive = true
                    }
                };

                foreach (var assignment in sampleAssignments)
                {
                    await context.ServiceUsers.AddAsync(assignment);
                }
            }
        }
    }
}
