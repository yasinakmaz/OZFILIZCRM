namespace CRM.Enums
{
    public enum ServiceStatus
    {
        /// <summary>
        /// Yeni oluşturulmuş, henüz kimseye atanmamış servis
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Personel atanmış ve aktif olarak çalışılan servis
        /// </summary>
        InProgress = 2,

        /// <summary>
        /// Tamamlanmış ama henüz admin onayı bekleyen servis
        /// </summary>
        WaitingApproval = 3,

        /// <summary>
        /// Admin tarafından onaylanmış, faturalandırılabilir servis
        /// </summary>
        Completed = 4,

        /// <summary>
        /// İptal edilmiş servis
        /// </summary>
        Cancelled = 5
    }
}
