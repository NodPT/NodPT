namespace NodPT.Data.DTOs
{
    public class ChatResponseDto
    {
        public int Id { get; set; }
        public int ChatMessageId { get; set; }
        public string? Action { get; set; } // "like", "dislike", "regenerate"
        public DateTime Timestamp { get; set; }
    }
}