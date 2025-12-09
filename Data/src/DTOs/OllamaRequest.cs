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

        /// <summary>
        /// Response format for TensorRT-LLM structured output
        /// Supports json_object and json_schema modes
        /// Note: TensorRT-LLM uses this instead of Ollama's "format" property
        /// </summary>
        [JsonPropertyName("response_format")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResponseFormat? response_format { get; set; }

        /// <summary>
        /// Tools for TensorRT-LLM function calling (OpenAI-style)
        /// Optional property for tool/function calling support
        /// </summary>
        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Tool>? tools { get; set; }

        /// <summary>
        /// Tool choice strategy for TensorRT-LLM
        /// Can be "auto", "none", or a specific tool name object
        /// </summary>
        [JsonPropertyName("tool_choice")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? tool_choice { get; set; }
    }
}
