using Microsoft.Extensions.Logging;

namespace CRM.Services.CrossCutting
{
    public class EncryptionService : IEncryptionService
    {
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(ILogger<EncryptionService> logger)
        {
            _logger = logger;
        }

        public string Encrypt(string plainText)
        {
            try
            {
                // Implementation: AES encryption
                var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data");
                return string.Empty;
            }
        }

        public string Decrypt(string cipherText)
        {
            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data");
                return string.Empty;
            }
        }

        public string GenerateSecureToken(int length = 32)
        {
            try
            {
                using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating secure token");
                return Guid.NewGuid().ToString("N");
            }
        }

        public bool VerifyHash(string input, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(input, hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying hash");
                return false;
            }
        }
    }
}
