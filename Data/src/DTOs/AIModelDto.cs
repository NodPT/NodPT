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

        // Ollama API endpoint and parameters
        public string? EndpointAddress { get; set; }
        public double? Temperature { get; set; }
        public int? NumPredict { get; set; }
        public int? TopK { get; set; }
        public double? TopP { get; set; }
        public int? Seed { get; set; }
        public int? NumCtx { get; set; }
        public int? NumGpu { get; set; }
        public int? NumThread { get; set; }
        public double? RepeatPenalty { get; set; }
        public string? Stop { get; set; }
    }
}
