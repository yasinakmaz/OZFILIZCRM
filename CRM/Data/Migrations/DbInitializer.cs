using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.Data.Migrations
{
    /// <summary>
    /// Veritabanı ilk verilerini yükleyen servis
    /// Seed data ve default configuration
    /// </summary>
    public class DbInitializer
    {
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(ILogger<DbInitializer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Veritabanına ilk verileri yükler
        /// </summary>
        public async Task InitializeAsync(TeknikServisDbContext context, bool isDevelopment = false)
        {
            try
            {
                _logger.LogInformation("🌱 Veritabanı seed işlemi başlatılıyor...");

                // **SUPER ADMIN USER**
                await SeedSuperAdminAsync(context);

                // **DEFAULT ROLES AND USERS**
                await SeedDefaultUsersAsync(context, isDevelopment);

                if (isDevelopment)
                {
                    // **DEVELOPMENT SAMPLE DATA**
                    await SeedSampleDataAsync(context);
                    _logger.LogInformation("🧪 Development örnek verileri yüklendi");
                }

                // **SAVE ALL CHANGES**
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Veritabanı seed işlemi tamamlandı!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Seed işlemi sırasında hata oluştu!");
                throw;
            }
        }

        /// <summary>
        /// Super admin kullanıcısını oluşturur
        /// </summary>
        private async Task SeedSuperAdminAsync(TeknikServisDbContext context)
        {
            const string defaultAdminUsername = "admin";
            const string defaultAdminPassword = "Admin123!";

            var existingAdmin = await context.Users
                .FirstOrDefaultAsync(u => u.Username == defaultAdminUsername);

            if (existingAdmin == null)
            {
                var adminUser = new User
                {
                    Username = defaultAdminUsername,
                    Email = "admin@teknikservis.com",
                    PasswordHash = BCrypt.HashPassword(defaultAdminPassword),
                    FirstName = "Sistem",
                    LastName = "Yöneticisi",
                    Role = UserRole.SuperAdmin,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                context.Users.Add(adminUser);

                _logger.LogInformation("👑 Super admin oluşturuldu: {Username} / {Password}",
                    defaultAdminUsername, defaultAdminPassword);
            }
            else
            {
                _logger.LogInformation("👑 Super admin zaten mevcut: {Username}", existingAdmin.Username);
            }
        }

        /// <summary>
        /// Varsayılan kullanıcıları oluşturur
        /// </summary>
        private async Task SeedDefaultUsersAsync(TeknikServisDbContext context, bool isDevelopment)
        {
            // **DEFAULT ADMIN**
            await CreateUserIfNotExistsAsync(context, new User
            {
                Username = "manager",
                Email = "manager@teknikservis.com",
                PasswordHash = BCrypt.HashPassword("Manager123!"),
                FirstName = "Sistem",
                LastName = "Müdürü",
                Role = UserRole.Admin,
                IsActive = true
            });

            if (isDevelopment)
            {
                // **SAMPLE TECHNICIANS**
                await CreateUserIfNotExistsAsync(context, new User
                {
                    Username = "teknisyen1",
                    Email = "teknisyen1@teknikservis.com",
                    PasswordHash = BCrypt.HashPassword("Tech123!"),
                    FirstName = "Ahmet",
                    LastName = "Yılmaz",
                    PhoneNumber = "0532-111-1111",
                    Role = UserRole.Technician,
                    IsActive = true
                });

                await CreateUserIfNotExistsAsync(context, new User
                {
                    Username = "teknisyen2",
                    Email = "teknisyen2@teknikservis.com",
                    PasswordHash = BCrypt.HashPassword("Tech123!"),
                    FirstName = "Fatma",
                    LastName = "Kaya",
                    PhoneNumber = "0532-222-2222",
                    Role = UserRole.Technician,
                    IsActive = true
                });

                // **SAMPLE CUSTOMER REPRESENTATIVE**
                await CreateUserIfNotExistsAsync(context, new User
                {
                    Username = "musteri1",
                    Email = "musteri1@teknikservis.com",
                    PasswordHash = BCrypt.HashPassword("Customer123!"),
                    FirstName = "Zeynep",
                    LastName = "Demir",
                    PhoneNumber = "0532-333-3333",
                    Role = UserRole.CustomerRepresentative,
                    IsActive = true
                });
            }
        }

        /// <summary>
        /// Development ortamı için örnek verileri oluşturur
        /// </summary>
        private async Task SeedSampleDataAsync(TeknikServisDbContext context)
        {
            // **SAMPLE CUSTOMERS**
            await CreateCustomerIfNotExistsAsync(context, new Customer
            {
                CompanyName = "ABC Teknoloji Ltd. Şti.",
                TaxNumber = "1234567890",
                ContactPerson = "Mehmet Özkan",
                Email = "info@abcteknoloji.com",
                PhoneNumber = "0212-555-0001",
                MobileNumber = "0532-555-0001",
                Address = "Maslak Mahallesi, Teknoloji Caddesi No:1",
                City = "İstanbul",
                District = "Sarıyer",
                PostalCode = "34398",
                CustomerType = CustomerType.Corporate,
                IsActive = true,
                CreditLimit = 50000,
                PaymentTermDays = 30
            });

            await CreateCustomerIfNotExistsAsync(context, new Customer
            {
                CompanyName = "XYZ Elektronik A.Ş.",
                TaxNumber = "9876543210",
                ContactPerson = "Ayşe Kılıç",
                Email = "info@xyzelektronik.com",
                PhoneNumber = "0312-555-0002",
                MobileNumber = "0533-555-0002",
                Address = "Kızılay Mahallesi, Elektronik Sokak No:15",
                City = "Ankara",
                District = "Çankaya",
                PostalCode = "06420",
                CustomerType = CustomerType.Corporate,
                IsActive = true,
                CreditLimit = 75000,
                PaymentTermDays = 45
            });

            await CreateCustomerIfNotExistsAsync(context, new Customer
            {
                CompanyName = "Bireysel Müşteri - Can Yıldız",
                ContactPerson = "Can Yıldız",
                Email = "can.yildiz@email.com",
                PhoneNumber = "0232-555-0003",
                MobileNumber = "0534-555-0003",
                Address = "Alsancak Mahallesi, Deniz Caddesi No:45",
                City = "İzmir",
                District = "Konak",
                PostalCode = "35220",
                CustomerType = CustomerType.Individual,
                IsActive = true,
                PaymentTermDays = 15
            });

            // **SAMPLE SERVICES**
            var customers = await context.Customers.ToListAsync();
            var technicians = await context.Users.Where(u => u.Role == UserRole.Technician).ToListAsync();

            if (customers.Any() && technicians.Any())
            {
                await CreateServiceIfNotExistsAsync(context, new Service
                {
                    Title = "Laptop Ekran Değişimi",
                    Description = "Dell Latitude 5520 laptop ekranında çatlak var, LCD panel değişimi gerekiyor.",
                    CustomerId = customers[0].Id,
                    AssignedUserId = technicians.FirstOrDefault()?.Id,
                    Status = ServiceStatus.InProgress,
                    Priority = Priority.High,
                    DeviceModel = "Dell Latitude 5520",
                    DeviceSerialNumber = "DL55200001",
                    ProblemDescription = "Ekranda dikey çizgiler ve çatlaklar mevcut",
                    ServiceAmount = 1500,
                    IsWarrantyService = false,
                    ExpectedCompletionDate = DateTime.Now.AddDays(3)
                });

                await CreateServiceIfNotExistsAsync(context, new Service
                {
                    Title = "Sunucu Bakım Hizmeti",
                    Description = "HP ProLiant sunucusunda periyodik bakım ve güncelleme işlemleri.",
                    CustomerId = customers[1].Id,
                    AssignedUserId = technicians.LastOrDefault()?.Id,
                    Status = ServiceStatus.Pending,
                    Priority = Priority.Normal,
                    DeviceModel = "HP ProLiant DL380",
                    DeviceSerialNumber = "HP380001",
                    ProblemDescription = "Aylık bakım zamanı geldi",
                    ServiceAmount = 2500,
                    IsWarrantyService = true,
                    ExpectedCompletionDate = DateTime.Now.AddDays(7)
                });

                await CreateServiceIfNotExistsAsync(context, new Service
                {
                    Title = "Yazıcı Tamiri",
                    Description = "Canon imageRUNNER yazıcıda kağıt sıkışması ve toner problemi.",
                    CustomerId = customers[2].Id,
                    Status = ServiceStatus.Completed,
                    Priority = Priority.Low,
                    DeviceModel = "Canon imageRUNNER 2530i",
                    DeviceSerialNumber = "CN253001",
                    ProblemDescription = "Kağıt sıkışması ve toner tanınmıyor",
                    SolutionDescription = "Kağıt yolu temizlendi, toner değiştirildi",
                    ServiceAmount = 800,
                    IsWarrantyService = false,
                    ServiceStartDateTime = DateTime.Now.AddDays(-5),
                    ServiceEndDateTime = DateTime.Now.AddDays(-2)
                });
            }
        }

        // **HELPER METHODS**

        private async Task CreateUserIfNotExistsAsync(TeknikServisDbContext context, User user)
        {
            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.Username == user.Username);

            if (existingUser == null)
            {
                user.CreatedDate = DateTime.Now;
                context.Users.Add(user);
                _logger.LogDebug("👤 Kullanıcı oluşturuldu: {Username}", user.Username);
            }
        }

        private async Task CreateCustomerIfNotExistsAsync(TeknikServisDbContext context, Customer customer)
        {
            var existingCustomer = await context.Customers
                .FirstOrDefaultAsync(c => c.CompanyName == customer.CompanyName);

            if (existingCustomer == null)
            {
                customer.CreatedDate = DateTime.Now;
                context.Customers.Add(customer);
                _logger.LogDebug("🏢 Müşteri oluşturuldu: {CompanyName}", customer.CompanyName);
            }
        }

        private async Task CreateServiceIfNotExistsAsync(TeknikServisDbContext context, Service service)
        {
            var existingService = await context.Services
                .FirstOrDefaultAsync(s => s.Title == service.Title && s.CustomerId == service.CustomerId);

            if (existingService == null)
            {
                service.CreatedDate = DateTime.Now;
                context.Services.Add(service);
                _logger.LogDebug("🔧 Servis oluşturuldu: {Title}", service.Title);
            }
        }
    }
}
