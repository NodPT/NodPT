using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Response model for Ollama generate API
    /// </summary>
    public class OllamaResponse
    {
        public string? model { get; set; }

        public string? created_at { get; set; }

        public string? response { get; set; }

        public bool done { get; set; }
    }
}
