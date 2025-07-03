namespace CRM.Services.CrossCutting
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        string GenerateSecureToken(int length = 32);
        bool VerifyHash(string input, string hash);
    }
}
