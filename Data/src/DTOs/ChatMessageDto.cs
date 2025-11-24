namespace NodPT.Data.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string? Sender { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool MarkedAsSolution { get; set; }
        public string? NodeId { get; set; }
        public bool Liked { get; set; }
        public bool Disliked { get; set; }
        public string? ConnectionId { get; set; }
    }
}
