using System.Text;

namespace CRM.Services
{
    /// <summary>
    /// PDF oluşturma ve yönetimi servisi
    /// Servis raporları, faturalar ve belgeler için kullanılır
    /// </summary>
    public class PdfService
    {
        private readonly LoggingService _loggingService;

        public PdfService(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Servis raporu PDF'i oluşturur
        /// İmza alanı ve müşteri bilgileri dahil
        /// </summary>
        public async Task<byte[]> GenerateServiceReportAsync(Service service)
        {
            try
            {
                var html = await GenerateServiceReportHtmlAsync(service);

                using var memoryStream = new MemoryStream();

                // HTML'i PDF'e çevir
                HtmlConverter.ConvertToPdf(html, memoryStream);

                await _loggingService.LogAsync(
                    "GENERATE_SERVICE_PDF",
                    "Service",
                    service.Id,
                    $"Servis #{service.Id} için PDF raporu oluşturuldu",
                    userId: null);

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GENERATE_SERVICE_PDF", "Service", service.Id);
                throw;
            }
        }

        /// <summary>
        /// Servis raporu için HTML şablonu oluşturur
        /// </summary>
        private async Task<string> GenerateServiceReportHtmlAsync(Service service)
        {
            var html = new StringBuilder();

            html.Append(@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Servis Raporu</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            font-size: 12px;
            line-height: 1.4;
        }
        .header {
            text-align: center;
            border-bottom: 2px solid #007bff;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }
        .company-name {
            font-size: 24px;
            font-weight: bold;
            color: #007bff;
            margin-bottom: 5px;
        }
        .company-subtitle {
            font-size: 14px;
            color: #666;
            margin-bottom: 10px;
        }
        .report-title {
            font-size: 18px;
            font-weight: bold;
            margin-top: 20px;
        }
        .info-section {
            margin-bottom: 25px;
        }
        .info-title {
            font-size: 14px;
            font-weight: bold;
            color: #007bff;
            margin-bottom: 10px;
            border-bottom: 1px solid #ddd;
            padding-bottom: 5px;
        }
        .info-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 20px;
        }
        .info-item {
            margin-bottom: 8px;
        }
        .info-label {
            font-weight: bold;
            color: #333;
            display: inline-block;
            width: 120px;
        }
        .info-value {
            color: #666;
        }
        .tasks-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 30px;
        }
        .tasks-table th,
        .tasks-table td {
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }
        .tasks-table th {
            background-color: #f8f9fa;
            font-weight: bold;
        }
        .task-completed {
            color: #28a745;
            font-weight: bold;
        }
        .task-pending {
            color: #dc3545;
            font-weight: bold;
        }
        .priority-critical {
            color: #dc3545;
            font-weight: bold;
        }
        .priority-high {
            color: #fd7e14;
            font-weight: bold;
        }
        .priority-normal {
            color: #007bff;
        }
        .priority-low {
            color: #6c757d;
        }
        .signature-section {
            margin-top: 50px;
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 100px;
        }
        .signature-box {
            text-align: center;
            border-top: 1px solid #000;
            padding-top: 10px;
            margin-top: 60px;
        }
        .footer {
            margin-top: 40px;
            text-align: center;
            color: #666;
            font-size: 10px;
            border-top: 1px solid #ddd;
            padding-top: 10px;
        }
        .amount-highlight {
            background-color: #fff3cd;
            padding: 10px;
            border-radius: 5px;
            border: 1px solid #ffeaa7;
            text-align: center;
            margin: 20px 0;
        }
        .status-badge {
            display: inline-block;
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 11px;
            font-weight: bold;
            text-transform: uppercase;
        }
        .status-completed {
            background-color: #d4edda;
            color: #155724;
        }
        .status-pending {
            background-color: #fff3cd;
            color: #856404;
        }
        .status-progress {
            background-color: #cce7ff;
            color: #004085;
        }
    </style>
</head>
<body>");

            // Header
            html.Append(@"
    <div class='header'>
        <div class='company-name'>TEKNİK SERVİS A.Ş.</div>
        <div class='company-subtitle'>Teknik Servis ve Destek Hizmetleri</div>
        <div class='report-title'>SERVİS RAPORU</div>
    </div>");

            // Servis Bilgileri
            html.Append($@"
    <div class='info-section'>
        <div class='info-title'>SERVİS BİLGİLERİ</div>
        <div class='info-grid'>
            <div>
                <div class='info-item'>
                    <span class='info-label'>Servis No:</span>
                    <span class='info-value'>#{service.Id}</span>
                </div>
                <div class='info-item'>
                    <span class='info-label'>Oluşturulma:</span>
                    <span class='info-value'>{service.CreatedDate:dd.MM.yyyy HH:mm}</span>
                </div>
                <div class='info-item'>
                    <span class='info-label'>Planlanan:</span>
                    <span class='info-value'>{service.ScheduledDateTime:dd.MM.yyyy HH:mm}</span>
                </div>
                {(service.ServiceStartDateTime.HasValue ? $@"
                <div class='info-item'>
                    <span class='info-label'>Başlangıç:</span>
                    <span class='info-value'>{service.ServiceStartDateTime:dd.MM.yyyy HH:mm}</span>
                </div>" : "")}
                {(service.ServiceEndDateTime.HasValue ? $@"
                <div class='info-item'>
                    <span class='info-label'>Bitiş:</span>
                    <span class='info-value'>{service.ServiceEndDateTime:dd.MM.yyyy HH:mm}</span>
                </div>" : "")}
            </div>
            <div>
                <div class='info-item'>
                    <span class='info-label'>Durum:</span>
                    <span class='status-badge {GetStatusCssClass(service.Status)}'>{GetStatusDisplayName(service.Status)}</span>
                </div>
                <div class='info-item'>
                    <span class='info-label'>Oluşturan:</span>
                    <span class='info-value'>{service.CreatedByUser.Username}</span>
                </div>
                {(service.ApprovedByUser != null ? $@"
                <div class='info-item'>
                    <span class='info-label'>Onaylayan:</span>
                    <span class='info-value'>{service.ApprovedByUser.Username}</span>
                </div>" : "")}
            </div>
        </div>
    </div>");

            // Müşteri Bilgileri
            html.Append($@"
    <div class='info-section'>
        <div class='info-title'>MÜŞTERİ BİLGİLERİ</div>
        <div class='info-grid'>
            <div>
                <div class='info-item'>
                    <span class='info-label'>Firma Adı:</span>
                    <span class='info-value'>{service.Customer.CompanyName}</span>
                </div>
                <div class='info-item'>
                    <span class='info-label'>Yetkili Kişi:</span>
                    <span class='info-value'>{service.Customer.AuthorizedPersonName}</span>
                </div>
                <div class='info-item'>
                    <span class='info-label'>Telefon:</span>
                    <span class='info-value'>{service.Customer.PhoneNumber}</span>
                </div>
            </div>
            <div>
                <div class='info-item'>
                    <span class='info-label'>Firma Tipi:</span>
                    <span class='info-value'>{(service.Customer.CompanyType == CompanyType.Corporate ? "Kurumsal" : "Bireysel")}</span>
                </div>
                <div class='info-item'>
                    <span class='info-label'>Vergi No/TCKN:</span>
                    <span class='info-value'>{service.Customer.TaxNumber}</span>
                </div>
                {(!string.IsNullOrEmpty(service.Customer.TaxOffice) ? $@"
                <div class='info-item'>
                    <span class='info-label'>Vergi Dairesi:</span>
                    <span class='info-value'>{service.Customer.TaxOffice}</span>
                </div>" : "")}
            </div>
        </div>
    </div>");

            // Tutar Bilgisi
            if (service.ServiceAmount.HasValue)
            {
                html.Append($@"
    <div class='amount-highlight'>
        <strong>SERVİS TUTARI: {service.ServiceAmount.Value:C}</strong>
    </div>");
            }

            // Yapılan İşler
            if (service.ServiceTasks.Any())
            {
                html.Append(@"
    <div class='info-section'>
        <div class='info-title'>YAPILAN İŞLER</div>
        <table class='tasks-table'>
            <thead>
                <tr>
                    <th>Açıklama</th>
                    <th>Öncelik</th>
                    <th>Durum</th>
                    <th>Tamamlayan</th>
                    <th>Tarih</th>
                </tr>
            </thead>
            <tbody>");

                foreach (var task in service.ServiceTasks.OrderBy(t => t.CreatedDate))
                {
                    html.Append($@"
                <tr>
                    <td>{task.Description}</td>
                    <td class='{GetPriorityCssClass(task.Priority)}'>{GetPriorityDisplayName(task.Priority)}</td>
                    <td class='{(task.IsCompleted ? "task-completed" : "task-pending")}'>
                        {(task.IsCompleted ? "Tamamlandı" : "Bekliyor")}
                    </td>
                    <td>{task.CompletedByUser?.Username ?? "-"}</td>
                    <td>{(task.CompletedDate?.ToString("dd.MM.yyyy") ?? "-")}</td>
                </tr>");
                }

                html.Append(@"
            </tbody>
        </table>
    </div>");
            }

            // Notlar
            if (!string.IsNullOrEmpty(service.Notes))
            {
                html.Append($@"
    <div class='info-section'>
        <div class='info-title'>NOTLAR</div>
        <p>{service.Notes}</p>
    </div>");
            }

            // İmza Alanları
            html.Append(@"
    <div class='signature-section'>
        <div>
            <div class='signature-box'>
                MÜŞTERİ İMZASI
            </div>
        </div>
        <div>
            <div class='signature-box'>
                TEKNİSYEN İMZASI
            </div>
        </div>
    </div>");

            // Footer
            html.Append($@"
    <div class='footer'>
        Bu rapor {DateTime.Now:dd.MM.yyyy HH:mm} tarihinde otomatik olarak oluşturulmuştur.<br>
        Teknik Servis A.Ş. | www.teknikservis.com | destek@teknikservis.com
    </div>

</body>
</html>");

            return html.ToString();
        }

        /// <summary>
        /// Fatura PDF'i oluşturur
        /// </summary>
        public async Task<byte[]> GenerateInvoiceAsync(Service service, string invoiceNumber)
        {
            try
            {
                var html = await GenerateInvoiceHtmlAsync(service, invoiceNumber);

                using var memoryStream = new MemoryStream();
                HtmlConverter.ConvertToPdf(html, memoryStream);

                await _loggingService.LogAsync(
                    "GENERATE_INVOICE_PDF",
                    "Service",
                    service.Id,
                    $"Servis #{service.Id} için fatura PDF'i oluşturuldu - Fatura No: {invoiceNumber}",
                    userId: null);

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, "GENERATE_INVOICE_PDF", "Service", service.Id);
                throw;
            }
        }

        /// <summary>
        /// Fatura HTML şablonu oluşturur
        /// </summary>
        private async Task<string> GenerateInvoiceHtmlAsync(Service service, string invoiceNumber)
        {
            // Fatura HTML şablonu burada oluşturulacak
            // Basit bir template için şimdilik servis raporu kullanıyoruz
            var html = await GenerateServiceReportHtmlAsync(service);
            return html.Replace("SERVİS RAPORU", $"FATURA - {invoiceNumber}");
        }

        // Helper metodları
        private string GetStatusCssClass(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Completed => "status-completed",
                ServiceStatus.InProgress => "status-progress",
                _ => "status-pending"
            };
        }

        private string GetStatusDisplayName(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Pending => "Bekleyen",
                ServiceStatus.InProgress => "Devam Ediyor",
                ServiceStatus.WaitingApproval => "Onay Bekliyor",
                ServiceStatus.Completed => "Tamamlandı",
                ServiceStatus.Cancelled => "İptal Edildi",
                _ => "Bilinmiyor"
            };
        }

        private string GetPriorityCssClass(Priority priority)
        {
            return priority switch
            {
                Priority.Critical => "priority-critical",
                Priority.High => "priority-high",
                Priority.Normal => "priority-normal",
                Priority.Low => "priority-low",
                _ => "priority-normal"
            };
        }

        private string GetPriorityDisplayName(Priority priority)
        {
            return priority switch
            {
                Priority.Low => "Düşük",
                Priority.Normal => "Normal",
                Priority.High => "Yüksek",
                Priority.Critical => "Kritik",
                _ => "Normal"
            };
        }
    }
}
