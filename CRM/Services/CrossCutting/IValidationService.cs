namespace CRM.Services.CrossCutting
{
    public interface IValidationService
    {
        Task<ValidationResult> ValidateAsync<T>(T model);
        ValidationResult ValidateEmail(string email);
        ValidationResult ValidatePhoneNumber(string phoneNumber);
        ValidationResult ValidateTaxNumber(string taxNumber);
    }
}
