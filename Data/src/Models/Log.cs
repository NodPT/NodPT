using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NodPT.Data.Models
{
    public class Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? ErrorMessage { get; set; }

        public string? StackTrace { get; set; }

        [MaxLength(255)]
        public string? Username { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Controller { get; set; }

        [MaxLength(255)]
        public string? Action { get; set; }

        [MaxLength(50)]
        public string? Level { get; set; }

        [MaxLength(255)]
        public string? Logger { get; set; }

        [MaxLength(255)]
        public string? Source { get; set; }
    }
}
