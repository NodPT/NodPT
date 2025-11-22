using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Many-to-one relationship: Project belongs to one User
        /// </summary>
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [JsonIgnore]
        public virtual User? User { get; set; }

        /// <summary>
        /// Many-to-one relationship: Project uses one Template
        /// </summary>
        public int? TemplateId { get; set; }

        [ForeignKey(nameof(TemplateId))]
        [JsonIgnore]
        public virtual Template? Template { get; set; }

        /// <summary>
        /// One-to-many relationship: Project has many Nodes
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();

        /// <summary>
        /// One-to-many relationship: Project has many Folders
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    }
}