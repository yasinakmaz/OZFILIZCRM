namespace CRM.DTOs
{
    /// <summary>
    /// Service result için generic wrapper
    /// API response'larında kullanılır
    /// </summary>
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        public static ServiceResult<T> Failure(string errorMessage)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        public static ServiceResult<T> ValidationFailure(List<string> errors)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                ValidationErrors = errors,
                ErrorMessage = "Validasyon hataları oluştu"
            };
        }
    }
}
