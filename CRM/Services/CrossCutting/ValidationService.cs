using Microsoft.Extensions.Logging;

namespace CRM.Services.CrossCutting
{
    public class ValidationService : IValidationService
    {
        public async Task<ValidationResult> ValidateAsync<T>(T model)
        {
            try
            {
                // Implementation: FluentValidation, DataAnnotations, etc.
                await Task.CompletedTask;
                return ValidationResult.Valid();
            }
            catch (Exception)
            {
                return ValidationResult.Invalid("Validation error");
            }
        }

        public ValidationResult ValidateEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email ? ValidationResult.Valid() : ValidationResult.Invalid("Invalid email format");
            }
            catch
            {
                return ValidationResult.Invalid("Invalid email format");
            }
        }

        public ValidationResult ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return ValidationResult.Invalid("Phone number required");

            // Turkish phone number validation
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (cleaned.StartsWith("+90"))
                cleaned = cleaned[3..];
            if (cleaned.StartsWith("0"))
                cleaned = cleaned[1..];

            return cleaned.Length == 10 && cleaned.All(char.IsDigit)
                ? ValidationResult.Valid()
                : ValidationResult.Invalid("Invalid phone number format");
        }

        public ValidationResult ValidateTaxNumber(string taxNumber)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
                return ValidationResult.Invalid("Tax number required");

            var cleaned = taxNumber.Trim();
            return (cleaned.Length == 10 || cleaned.Length == 11) && cleaned.All(char.IsDigit)
                ? ValidationResult.Valid()
                : ValidationResult.Invalid("Tax number must be 10-11 digits");
        }
    }

    // **PERFORMANCE MONITOR**
    public interface IPerformanceMonitor
    {
        Task<TimeSpan> MeasureAsync(Func<Task> operation, string operationName);
        Task<T> MeasureAsync<T>(Func<Task<T>> operation, string operationName);
        Task LogPerformanceMetricAsync(string operationName, TimeSpan duration);
    }

    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger;
        }

        public async Task<TimeSpan> MeasureAsync(Func<Task> operation, string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await operation();
                return stopwatch.Elapsed;
            }
            finally
            {
                stopwatch.Stop();
                await LogPerformanceMetricAsync(operationName, stopwatch.Elapsed);
            }
        }

        public async Task<T> MeasureAsync<T>(Func<Task<T>> operation, string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                return await operation();
            }
            finally
            {
                stopwatch.Stop();
                await LogPerformanceMetricAsync(operationName, stopwatch.Elapsed);
            }
        }

        public async Task LogPerformanceMetricAsync(string operationName, TimeSpan duration)
        {
            try
            {
                if (duration.TotalMilliseconds > 1000) // Log slow operations
                {
                    _logger.LogWarning("Slow operation detected: {OperationName} took {Duration}ms",
                        operationName, duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogDebug("Operation {OperationName} completed in {Duration}ms",
                        operationName, duration.TotalMilliseconds);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging performance metric");
            }
        }
    }
}
