using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Request model for Ollama generate API
    /// </summary>
    public class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? prompt { get; set; }

        [JsonPropertyName("stream")]
        public bool stream { get; private set; } = false;

        [JsonPropertyName("options")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public OllamaOptions? options { get; set; }

        [JsonPropertyName("messages")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<OllamaMessage>? messages { get; set; }

        [JsonPropertyName("images")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? images { get; set; }
    }
}
