using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NodPT.Data.Models
{
    public class UserAccessLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [MaxLength(100)]
        public string? Action { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool Success { get; set; } = true;

        public string? ErrorMessage { get; set; }
    }
}