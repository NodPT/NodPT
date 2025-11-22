using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Prompt
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? Content { get; set; }

        public MessageTypeEnum MessageType { get; set; }

        public LevelEnum Level { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Many-to-one relationship: Prompt belongs to a Template
        /// </summary>
        public int? TemplateId { get; set; }

        [ForeignKey(nameof(TemplateId))]
        [JsonIgnore]
        public virtual Template? Template { get; set; }
    }
}
