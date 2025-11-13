namespace NodPT.Data.DTOs
{
    public class TemplateFileDto
    {
        public int Oid { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Extension { get; set; }
        public long Size { get; set; }
        public string? MimeType { get; set; }
        public string? Content { get; set; }
        public int? TemplateId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTemplateFileDto
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Extension { get; set; }
        public long Size { get; set; }
        public string? MimeType { get; set; }
        public string? Content { get; set; }
        public int TemplateId { get; set; }
    }

    public class UpdateTemplateFileDto
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Extension { get; set; }
        public long Size { get; set; }
        public string? MimeType { get; set; }
        public string? Content { get; set; }
    }
}