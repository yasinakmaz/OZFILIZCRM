using System.ComponentModel.DataAnnotations;

namespace CRM.Enums
{
    /// <summary>
    /// Müşteri tipleri - Business classification
    /// </summary>
    public enum CustomerType
    {
        [Display(Name = "Kurumsal")]
        Corporate = 0,

        [Display(Name = "Bireysel")]
        Individual = 1,

        [Display(Name = "Bayi")]
        Dealer = 2
    }
}
