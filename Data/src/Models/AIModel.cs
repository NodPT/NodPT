using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class AIModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(255)]
        public string? ModelIdentifier { get; set; }

        public MessageTypeEnum MessageType { get; set; }

        public LevelEnum Level { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Many-to-one relationship: AIModel belongs to a Template
        /// </summary>
        public int? TemplateId { get; set; }

        [ForeignKey(nameof(TemplateId))]
        [JsonIgnore]
        public virtual Template? Template { get; set; }

        /// <summary>
        /// One-to-many relationship: AIModel can have many Nodes
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();
    }
}
