using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(128)]
        public string? FirebaseUid { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? DisplayName { get; set; }

        public string? PhotoUrl { get; set; }

        public bool Active { get; set; } = true;

        public bool Approved { get; set; } = false;

        public bool Banned { get; set; } = false;

        public bool IsAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        public string? RefreshToken { get; set; }

        /// <summary>
        /// One-to-many relationship: User has many ChatMessages
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// One-to-many relationship: User has many Projects
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

        /// <summary>
        /// One-to-many relationship: User has many AccessLogs
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<UserAccessLog> AccessLogs { get; set; } = new List<UserAccessLog>();
    }
}