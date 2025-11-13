namespace NodPT.Data.DTOs
{
    public class ChatResponseDto
    {
        public Guid Id { get; set; }
        public Guid ChatMessageId { get; set; }
        public string? Action { get; set; } // "like", "dislike", "regenerate"
        public DateTime Timestamp { get; set; }
    }
}