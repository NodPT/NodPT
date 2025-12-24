using NodPT.Data.Models;

namespace NodPT.Data.DTOs
{
    public class NodeDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public NodeType NodeType { get; set; } = NodeType.Worker;
        public Dictionary<string, string> Properties { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Status { get; set; }
        public string? ParentId { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public MessageTypeEnum MessageType { get; set; }
        public AIModelDto? MatchingAIModel { get; set; }
        public List<PromptDto> MatchingPrompts { get; set; } = new();
    }
}
