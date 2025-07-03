namespace CRM.Services.CrossCutting
{
    /// <summary>
    /// Global error handler interface
    /// Uygulama geneli hata yönetimi için
    /// </summary>
    public interface IGlobalErrorHandler
    {
        Task HandleErrorAsync(Exception exception, string? context = null);
        Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName);
        Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName);
        void ConfigureErrorBoundary(Action<Exception> onError);
    }
}
