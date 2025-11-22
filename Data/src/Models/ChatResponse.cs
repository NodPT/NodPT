using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NodPT.Data.Models
{
    public class ChatResponse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? ChatMessageId { get; set; }

        [ForeignKey(nameof(ChatMessageId))]
        [JsonIgnore]
        public virtual ChatMessage? ChatMessage { get; set; }

        [MaxLength(50)]
        public string? Role { get; set; }

        public string? Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}