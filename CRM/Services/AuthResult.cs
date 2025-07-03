namespace CRM.Services
{
    /// <summary>
    /// Authentication işlemleri için result class
    /// Success/failure durumlarını ve detayları içerir
    /// </summary>
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public static AuthResult Success(User user, string? token = null)
        {
            return new AuthResult
            {
                IsSuccess = true,
                User = user,
                Token = token,
                ExpiryDate = DateTime.Now.AddHours(24)
            };
        }

        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
