namespace CRM.Enums
{
    public enum UserRole
    {
        /// <summary>
        /// Teknisyen - sadece atandığı servisleri görebilir ve düzenleyebilir
        /// </summary>
        Technician = 1,

        /// <summary>
        /// Süpervizör - ekibini yönetir, servis atama yetkisi var
        /// </summary>
        Supervisor = 2,

        /// <summary>
        /// Yönetici - tüm servisler ve raporlar için tam yetki
        /// Kullanıcı yönetimi ve sistem ayarları erişimi var
        /// </summary>
        Admin = 3
    }
}
