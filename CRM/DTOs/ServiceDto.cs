using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRM.DTOs
{
    /// <summary>
    /// Servis form işlemleri için veri transfer objesi
    /// Validation attribute'ları ile client-side validation sağlar
    /// </summary>
    public class ServiceDto
    {
        /// <summary>
        /// Servis ID'si - güncellemeler için kullanılır
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Müşteri ID'si - Foreign Key
        /// </summary>
        [Required(ErrorMessage = "Müşteri seçilmelidir")]
        public int CustomerId { get; set; }

        /// <summary>
        /// Servis tutarı - opsiyonel, sonradan da eklenebilir
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Servis tutarı pozitif bir değer olmalıdır")]
        public decimal? ServiceAmount { get; set; }

        /// <summary>
        /// Planlanan servis tarihi ve saati
        /// </summary>
        [Required(ErrorMessage = "Planlanan tarih ve saat gereklidir")]
        public DateTime ScheduledDateTime { get; set; } = DateTime.Now.AddHours(1);

        /// <summary>
        /// Servis notları ve açıklamaları
        /// </summary>
        [StringLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir")]
        public string? Notes { get; set; }

        /// <summary>
        /// Servis görevleri listesi
        /// </summary>
        public List<ServiceTaskDto> Tasks { get; set; } = new List<ServiceTaskDto>();

        /// <summary>
        /// Atanacak kullanıcı ID'leri
        /// </summary>
        public List<int> AssignedUserIds { get; set; } = new List<int>();
    }
}
