using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Template
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(50)]
        public string? Version { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// One-to-many relationship: Template has many Projects
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

        /// <summary>
        /// One-to-many relationship: Template has many TemplateFiles
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<TemplateFile> TemplateFiles { get; set; } = new List<TemplateFile>();

        /// <summary>
        /// One-to-many relationship: Template has many Nodes
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();

        /// <summary>
        /// One-to-many relationship: Template has many Prompts
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();

        /// <summary>
        /// One-to-many relationship: Template has many AIModels
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<AIModel> AIModels { get; set; } = new List<AIModel>();
    }
}