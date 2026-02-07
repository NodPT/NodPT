namespace NodPT.Data.DTOs
{
    public class RetryMessageRequestDto
    {
        public string? NodeId { get; set; }
        public int? MessageId { get; set; }
        public string? ConnectionId { get; set; }
    }
}
