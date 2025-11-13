using NodPT.Data.Models;

namespace NodPT.Data.DTOs
{
    public class AIModelDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ModelIdentifier { get; set; }
        public MessageTypeEnum MessageType { get; set; }
        public LevelEnum Level { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? TemplateId { get; set; }
    }
}
