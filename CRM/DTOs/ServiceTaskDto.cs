using System.ComponentModel.DataAnnotations;


namespace CRM.DTOs
{
    /// <summary>
    /// Servis görevi için veri transfer objesi
    /// </summary>
    public class ServiceTaskDto
    {
        /// <summary>
        /// Görev ID'si
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Görev açıklaması
        /// </summary>
        [Required(ErrorMessage = "Görev açıklaması gereklidir")]
        [StringLength(500, ErrorMessage = "Görev açıklaması en fazla 500 karakter olabilir")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Görev öncelik seviyesi
        /// </summary>
        [Required(ErrorMessage = "Öncelik seviyesi seçilmelidir")]
        public Priority Priority { get; set; } = Priority.Normal;

        /// <summary>
        /// Görevin tamamlanıp tamamlanmadığı
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Tamamlanma tarihi
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Görevi tamamlayan kullanıcının adı
        /// </summary>
        public string? CompletedByUserName { get; set; }
    }
}
