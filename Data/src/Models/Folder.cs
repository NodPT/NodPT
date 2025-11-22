using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class Folder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Path { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Many-to-one relationship: Folder belongs to one Project
        /// </summary>
        public int? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [JsonIgnore]
        public virtual Project? Project { get; set; }

        /// <summary>
        /// Self-referencing relationship: Folder can have a parent folder
        /// </summary>
        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        [JsonIgnore]
        public virtual Folder? Parent { get; set; }

        /// <summary>
        /// Self-referencing relationship: Folder can have child folders
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Folder> Children { get; set; } = new List<Folder>();

        /// <summary>
        /// One-to-many relationship: Folder has many Files
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
    }
}