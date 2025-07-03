using System.ComponentModel.DataAnnotations;

namespace CRM.Enums
{
    /// <summary>
    /// Servis durumları - Workflow management
    /// </summary>
    public enum ServiceStatus
    {
        [Display(Name = "Beklemede")]
        Pending = 0,

        [Display(Name = "Kabul Edildi")]
        Accepted = 1,

        [Display(Name = "Devam Ediyor")]
        InProgress = 2,

        [Display(Name = "Parça Bekleniyor")]
        WaitingForParts = 3,

        [Display(Name = "Test Ediliyor")]
        Testing = 4,

        [Display(Name = "Tamamlandı")]
        Completed = 5,

        [Display(Name = "İptal Edildi")]
        Cancelled = 6,

        [Display(Name = "Reddedildi")]
        Rejected = 7
    }
}
