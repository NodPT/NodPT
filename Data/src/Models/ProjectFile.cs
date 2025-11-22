using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class ProjectFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Path { get; set; }

        [MaxLength(50)]
        public string? Extension { get; set; }

        public long Size { get; set; } = 0;

        [MaxLength(100)]
        public string? MimeType { get; set; }

        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Many-to-one relationship: File belongs to one Folder
        /// </summary>
        public int? FolderId { get; set; }

        [ForeignKey(nameof(FolderId))]
        [JsonIgnore]
        public virtual Folder? Folder { get; set; }
    }
}