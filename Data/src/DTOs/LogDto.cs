namespace NodPT.Data.DTOs
{
    public class LogDto
    {
        public int Id { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public string? Username { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
    }
}
