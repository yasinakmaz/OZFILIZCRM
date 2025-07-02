using System.Text;


namespace CRM.Services
{
    /// <summary>
    /// ESC/POS termal yazıcı servisi
    /// Makbuz ve fatura yazdırma için kullanılır
    /// </summary>
    public class PrintService
    {
        private readonly LoggingService _loggingService;

        public PrintService(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// ESC/POS komutları ile termal yazıcı çıktısı oluşturur
        /// </summary>
        public async Task<byte[]> GenerateThermalReceiptAsync(Service service)
        {
            try
            {
                var commands = new List<byte>();

                // ESC/POS komutları
                var ESC = 0x1B;
                var GS = 0x1D;

                // Yazıcı başlatma
                commands.AddRange(new byte[] { ESC, 0x40 }); // Initialize printer

                // Karakter seti (UTF-8)
                commands.AddRange(new byte[] { ESC, 0x74, 0x06 }); // Select character code table

                // Ortalama
                commands.AddRange(new byte[] { ESC, 0x61, 0x01 }); // Center alignment

                // Büyük font
                commands.AddRange(new byte[] { ESC, 0x21, 0x30 }); // Double height and width

                // Başlık
                var header = "TEKNIK SERVIS A.S.\n";
                commands.AddRange(Encoding.UTF8.GetBytes(header));

                // Normal font
                commands.AddRange(new byte[] { ESC, 0x21, 0x00 }); // Normal font

                // Alt başlık
                var subtitle = "Servis Makbuzu\n";
                commands.AddRange(Encoding.UTF8.GetBytes(subtitle));

                // Çizgi
                commands.AddRange(Encoding.UTF8.GetBytes("================================\n"));

                // Sol hizala
                commands.AddRange(new byte[] { ESC, 0x61, 0x00 }); // Left alignment

                // Servis bilgileri
                var serviceInfo = new StringBuilder();
                serviceInfo.AppendLine($"Servis No      : #{service.Id}");
                serviceInfo.AppendLine($"Tarih          : {service.CreatedDate:dd.MM.yyyy HH:mm}");
                serviceInfo.AppendLine($"Musteri        : {service.Customer.CompanyName}");
                serviceInfo.AppendLine($"Yetkili        : {service.Customer.AuthorizedPersonName}");
                serviceInfo.AppendLine($"Telefon        : {service.Customer.PhoneNumber}");
                serviceInfo.AppendLine($"Durum          : {GetStatusDisplayName(service.Status)}");

                if (service.ServiceAmount.HasValue)
                {
                    serviceInfo.AppendLine($"Tutar          : {service.ServiceAmount.Value:C}");
                }

                commands.AddRange(Encoding.UTF8.GetBytes(serviceInfo.ToString()));

                // Çizgi
                commands.AddRange(Encoding.UTF8.GetBytes("================================\n"));

                // Yapılan işler
                if (service.ServiceTasks.Any())
                {
                    commands.AddRange(Encoding.UTF8.GetBytes("YAPILAN ISLER:\n"));
                    commands.AddRange(Encoding.UTF8.GetBytes("--------------------------------\n"));

                    foreach (var task in service.ServiceTasks.OrderBy(t => t.CreatedDate))
                    {
                        var taskInfo = $"- {task.Description}\n";
                        if (task.IsCompleted)
                        {
                            taskInfo += $"  Tamamlandi: {task.CompletedDate:dd.MM.yyyy}\n";
                        }
                        else
                        {
                            taskInfo += "  Bekliyor\n";
                        }

                        commands.AddRange(Encoding.UTF8.GetBytes(taskInfo));
                    }

                    commands.AddRange(Encoding.UTF8.GetBytes("================================\n"));
                }

                // Notlar
                if (!string.IsNullOrEmpty(service.Notes))
                {
                    commands.AddRange(Encoding.UTF8.GetBytes("NOTLAR:\n"));
                    commands.AddRange(Encoding.UTF8.GetBytes($"{service.Notes}\n"));
                    commands.AddRange(Encoding.UTF8.GetBytes("================================\n"));
                }

                // Ortalama
                commands.AddRange(new byte[] { ESC, 0x61, 0x01 }); // Center alignment

                // Footer
                var footer = "\nTesekkur ederiz!\n";
                footer += "www.teknikservis.com\n";
                footer += "Tel: 0212 123 45 67\n\n";
                commands.AddRange(Encoding.UTF8.GetBytes(footer));

                // Kağıt kes
                commands.AddRange(new byte[] { GS, 0x56, 0x41, 0x10 }); // Cut paper

                await _loggingService.LogAsync(
                    "GENERATE_THERMAL_RECEIPT",
                    "Service",
                    service.Id,
                    $"Servis #{service.Id} için termal makbuz oluşturuldu",
                    userId: null);

                return commands.ToArray();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GENERATE_THERMAL_RECEIPT", "Service", service.Id);
                throw;
            }
        }

        /// <summary>
        /// Yazıcıya gönderilecek veriyi dosyaya kaydeder
        /// Test ve debug amaçlı
        /// </summary>
        public async Task SaveReceiptToFileAsync(byte[] receiptData, string filename)
        {
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, "TeknikServis", "Receipts", filename);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                await File.WriteAllBytesAsync(filePath, receiptData);

                await _loggingService.LogAsync(
                    "SAVE_RECEIPT_FILE",
                    "Print",
                    description: $"Makbuz dosyaya kaydedildi: {filePath}");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "SAVE_RECEIPT_FILE", "Print");
                throw;
            }
        }

        private string GetStatusDisplayName(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Pending => "Bekleyen",
                ServiceStatus.InProgress => "Devam Ediyor",
                ServiceStatus.WaitingApproval => "Onay Bekliyor",
                ServiceStatus.Completed => "Tamamlandi",
                ServiceStatus.Cancelled => "Iptal Edildi",
                _ => "Bilinmiyor"
            };
        }
    }
}
