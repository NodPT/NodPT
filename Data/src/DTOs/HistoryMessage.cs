namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Represents a message in the history with role and content.
    /// </summary>
    public class HistoryMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
