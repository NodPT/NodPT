using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{
    /// <summary>
    /// Response format configuration for TensorRT-LLM structured output
    /// Supports both json_object and json_schema modes
    /// </summary>
    public class ResponseFormat
    {
        /// <summary>
        /// Type of response format
        /// Accepted values: "json_object" or "json_schema"
        /// </summary>
        [JsonPropertyName("type")]
        public string? type { get; set; }

        /// <summary>
        /// JSON Schema definition for structured output (only used when type is "json_schema")
        /// </summary>
        [JsonPropertyName("schema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonSchema? schema { get; set; }
    }
}
