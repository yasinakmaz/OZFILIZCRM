namespace CRM.Services.CrossCutting
{
    /// <summary>
    /// Security servisi interface'i
    /// Data encryption, permission checking vb.
    /// </summary>
    public interface ISecurityService
    {
        Task<bool> HasPermissionAsync(int userId, string permission);
        Task<bool> CanAccessServiceAsync(int userId, int serviceId);
        Task<bool> CanAccessCustomerAsync(int userId, int customerId);
        string HashSensitiveData(string data);
        string EncryptSensitiveData(string data);
        string DecryptSensitiveData(string encryptedData);
        bool ValidateDataIntegrity(string data, string hash);
    }
}
