using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Response model for Ollama API (supports both /api/generate and /api/chat endpoints)
    /// </summary>
    public class OllamaResponse
    {
        public string? model { get; set; }

        public string? created_at { get; set; }

        /// <summary>
        /// Response content from /api/generate endpoint
        /// </summary>
        public string? response { get; set; }

        /// <summary>
        /// Message object from /api/chat endpoint
        /// </summary>
        public OllamaResponseMessage? message { get; set; }

        public bool done { get; set; }

        /// <summary>
        /// Gets the content from either response (generate) or message.content (chat)
        /// </summary>
        [JsonIgnore]
        public string Content => response ?? message?.content ?? string.Empty;
    }

    /// <summary>
    /// Message structure from /api/chat endpoint response
    /// </summary>
    public class OllamaResponseMessage
    {
        public string? role { get; set; }
        public string? content { get; set; }
    }
}
