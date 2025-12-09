using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{
    /// <summary>
    /// Tool definition for TensorRT-LLM function calling (OpenAI-style)
    /// </summary>
    public class Tool
    {
        /// <summary>
        /// Type of tool (currently only "function" is supported)
        /// </summary>
        [JsonPropertyName("type")]
        public string type { get; set; } = "function";

        /// <summary>
        /// Function definition
        /// </summary>
        [JsonPropertyName("function")]
        public ToolFunction? function { get; set; }
    }

    /// <summary>
    /// Function definition within a Tool
    /// </summary>
    public class ToolFunction
    {
        /// <summary>
        /// Name of the function
        /// </summary>
        [JsonPropertyName("name")]
        public string? name { get; set; }

        /// <summary>
        /// Description of what the function does
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? description { get; set; }

        /// <summary>
        /// Parameters schema (JSON Schema format)
        /// </summary>
        [JsonPropertyName("parameters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonSchema? parameters { get; set; }
    }
}
