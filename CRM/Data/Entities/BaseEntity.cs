using System.ComponentModel.DataAnnotations;


namespace CRM.Data.Entities
{
    /// <summary>
    /// Tüm entity'ler için ortak alanları içeren base class
    /// Audit trail ve soft delete pattern için kritik
    /// </summary>
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedDate { get; set; }
    }
}
