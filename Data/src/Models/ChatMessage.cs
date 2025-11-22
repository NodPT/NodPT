using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class ChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Sender { get; set; }

        public string? Message { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool MarkedAsSolution { get; set; }

        public bool Liked { get; set; }

        public bool Disliked { get; set; }

        public string? NodeId { get; set; }

        [ForeignKey(nameof(NodeId))]
        [JsonIgnore]
        public virtual Node? Node { get; set; }

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [JsonIgnore]
        public virtual User? User { get; set; }

        [JsonIgnore]
        public virtual ICollection<ChatResponse> ChatResponses { get; set; } = new List<ChatResponse>();
    }
}
