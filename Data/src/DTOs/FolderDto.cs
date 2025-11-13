namespace NodPT.Data.DTOs
{
    public class FolderDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? ProjectId { get; set; }
        public int? ParentId { get; set; }
        public string? ProjectName { get; set; }
        public string? ParentName { get; set; }
    }
}