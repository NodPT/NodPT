namespace NodPT.Data.DTOs
{
    public class ProjectFileDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Extension { get; set; }
        public long Size { get; set; } = 0;
        public string? MimeType { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? FolderId { get; set; }
        public string? FolderName { get; set; }
    }
}