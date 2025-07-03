using System.ComponentModel.DataAnnotations;

namespace CRM.Data.Entities
{
    // **ENUM DEFINITIONS**

    /// <summary>
    /// Kullanıcı rolleri - Authorization ve permission management
    /// </summary>
    public enum UserRole
    {
        [Display(Name = "Süper Admin")]
        SuperAdmin = 0,

        [Display(Name = "Admin")]
        Admin = 1,

        [Display(Name = "Teknisyen")]
        Technician = 2,

        [Display(Name = "Müşteri Temsilcisi")]
        CustomerRepresentative = 3,

        [Display(Name = "Kullanıcı")]
        User = 4
    }
}
