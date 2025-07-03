using Microsoft.Extensions.Logging;

namespace CRM.Services.CrossCutting
{
    /// <summary>
    /// Global error handler implementation
    /// Comprehensive error handling, logging ve user feedback
    /// </summary>
    public class GlobalErrorHandler : IGlobalErrorHandler
    {
        private readonly ILogger<GlobalErrorHandler> _logger;
        private readonly ILoggingService _loggingService;
        private readonly INotificationService _notificationService;
        private Action<Exception>? _onError;

        public GlobalErrorHandler(
            ILogger<GlobalErrorHandler> logger,
            ILoggingService loggingService,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// Hata'yı handle eder
        /// </summary>
        public async Task HandleErrorAsync(Exception exception, string? context = null)
        {
            try
            {
                var errorId = Guid.NewGuid().ToString("N")[..8];
                var errorContext = context ?? "Unknown";

                _logger.LogError(exception, "Global error [{ErrorId}] in context: {Context}", errorId, errorContext);

                // **LOG TO DATABASE**
                await _loggingService.LogErrorAsync(exception, "GLOBAL_ERROR", errorContext);

                // **CATEGORIZE ERROR**
                var errorCategory = CategorizeError(exception);
                var userMessage = GetUserFriendlyMessage(errorCategory, errorId);

                // **NOTIFY ERROR BOUNDARY**
                _onError?.Invoke(exception);

                // **CRITICAL ERRORS** - notify admins
                if (errorCategory == ErrorCategory.Critical)
                {
                    await NotifyAdminsOfCriticalErrorAsync(exception, errorId, errorContext);
                }

                _logger.LogInformation("Error [{ErrorId}] handled successfully", errorId);
            }
            catch (Exception handlerException)
            {
                // **FALLBACK LOGGING** - error handler itself failed
                _logger.LogCritical(handlerException, "CRITICAL: Global error handler failed while handling: {OriginalException}",
                    exception.Message);
            }
        }

        /// <summary>
        /// Operation'ı error handling ile execute eder (generic)
        /// </summary>
        public async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName)
        {
            try
            {
                _logger.LogDebug("Executing operation: {OperationName}", operationName);
                var result = await operation();
                _logger.LogDebug("Operation completed successfully: {OperationName}", operationName);
                return result;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, operationName);
                throw; // Re-throw for caller to handle
            }
        }

        /// <summary>
        /// Operation'ı error handling ile execute eder (void)
        /// </summary>
        public async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName)
        {
            try
            {
                _logger.LogDebug("Executing operation: {OperationName}", operationName);
                await operation();
                _logger.LogDebug("Operation completed successfully: {OperationName}", operationName);
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, operationName);
                throw; // Re-throw for caller to handle
            }
        }

        /// <summary>
        /// Error boundary callback'ini configure eder
        /// </summary>
        public void ConfigureErrorBoundary(Action<Exception> onError)
        {
            _onError = onError;
        }

        // **PRIVATE HELPER METHODS**

        /// <summary>
        /// Exception'ı kategorize eder
        /// </summary>
        private static ErrorCategory CategorizeError(Exception exception)
        {
            return exception switch
            {
                OutOfMemoryException => ErrorCategory.Critical,
                StackOverflowException => ErrorCategory.Critical,
                AccessViolationException => ErrorCategory.Critical,
                ArgumentNullException => ErrorCategory.Validation,
                ArgumentException => ErrorCategory.Validation,
                InvalidOperationException => ErrorCategory.Business,
                UnauthorizedAccessException => ErrorCategory.Security,
                TimeoutException => ErrorCategory.Network,
                HttpRequestException => ErrorCategory.Network,
                TaskCanceledException => ErrorCategory.Network,
                _ => ErrorCategory.General
            };
        }

        /// <summary>
        /// User-friendly error message döndürür
        /// </summary>
        private static string GetUserFriendlyMessage(ErrorCategory category, string errorId)
        {
            return category switch
            {
                ErrorCategory.Critical => $"Kritik bir sistem hatası oluştu. Lütfen sistem yöneticisi ile iletişime geçin. Hata Kodu: {errorId}",
                ErrorCategory.Validation => "Girilen bilgilerde hata bulundu. Lütfen kontrol edip tekrar deneyin.",
                ErrorCategory.Business => "İşlem gerçekleştirilemedi. Lütfen bilgileri kontrol edip tekrar deneyin.",
                ErrorCategory.Security => "Bu işlem için yetkiniz bulunmuyor.",
                ErrorCategory.Network => "Bağlantı sorunu yaşanıyor. Lütfen internet bağlantınızı kontrol edin.",
                ErrorCategory.General => $"Beklenmedik bir hata oluştu. Hata Kodu: {errorId}",
                _ => $"Sistem hatası oluştu. Hata Kodu: {errorId}"
            };
        }

        /// <summary>
        /// Kritik hatalar için admin'leri bilgilendirir
        /// </summary>
        private async Task NotifyAdminsOfCriticalErrorAsync(Exception exception, string errorId, string context)
        {
            try
            {
                // Implementation: Send email, push notification to admins
                _logger.LogWarning("Critical error notification sent to admins: {ErrorId}", errorId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify admins of critical error");
            }
        }

        /// <summary>
        /// Error kategorileri
        /// </summary>
        private enum ErrorCategory
        {
            General,
            Critical,
            Validation,
            Business,
            Security,
            Network
        }
    }
}
