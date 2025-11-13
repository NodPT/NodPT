namespace NodPT.Data.DTOs
{
    public class ProjectDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? UserId { get; set; }
        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public string? UserEmail { get; set; }
        public List<NodeDto> Nodes { get; set; } = new();
    }
}