namespace CRM.Enums
{
    public enum Priority
    {
        /// <summary>
        /// Düşük öncelik - acil değil
        /// </summary>
        Low = 1,

        /// <summary>
        /// Normal öncelik - standart iş akışı
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Yüksek öncelik - önemli
        /// </summary>
        High = 3,

        /// <summary>
        /// Kritik öncelik - acil müdahale gerektiren
        /// </summary>
        Critical = 4
    }
}
