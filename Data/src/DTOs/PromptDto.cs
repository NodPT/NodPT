using NodPT.Data.Models;

namespace NodPT.Data.DTOs
{
    public class PromptDto
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public MessageTypeEnum MessageType { get; set; }
        public LevelEnum Level { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? TemplateId { get; set; }
    }
}
