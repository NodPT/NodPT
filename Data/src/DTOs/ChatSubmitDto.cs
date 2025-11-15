namespace NodPT.Data.DTOs
{
    public class ChatSubmitDto
    {
        public string? UserId { get; set; }
        public string? ConnectionId { get; set; }
        public string? Message { get; set; }
        public string? ProjectId { get; set; }
        public string? NodeLevel { get; set; }
    }
}
