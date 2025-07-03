using System.ComponentModel.DataAnnotations;

namespace CRM.Enums
{
    /// <summary>
    /// Öncelik seviyeleri - Priority management
    /// </summary>
    public enum Priority
    {
        [Display(Name = "Düşük")]
        Low = 0,

        [Display(Name = "Normal")]
        Normal = 1,

        [Display(Name = "Yüksek")]
        High = 2,

        [Display(Name = "Kritik")]
        Critical = 3
    }
}
