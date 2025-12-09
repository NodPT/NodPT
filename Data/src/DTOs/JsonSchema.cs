using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{
    /// <summary>
    /// JSON Schema definition for TensorRT-LLM structured output
    /// This class represents a JSON Schema object that can be passed to TensorRT-LLM
    /// </summary>
    public class JsonSchema
    {
        /// <summary>
        /// Schema type (e.g., "object", "array", "string", "number", "boolean")
        /// </summary>
        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? type { get; set; }

        /// <summary>
        /// Properties definition for object types
        /// Key is property name, value is the schema for that property
        /// </summary>
        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, JsonSchema>? properties { get; set; }

        /// <summary>
        /// List of required property names
        /// </summary>
        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? required { get; set; }

        /// <summary>
        /// Schema for array items (used when type is "array")
        /// </summary>
        [JsonPropertyName("items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonSchema? items { get; set; }

        /// <summary>
        /// Enum values (used for constrained string types)
        /// </summary>
        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<object>? @enum { get; set; }

        /// <summary>
        /// Description of the schema or property
        /// </summary>
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? description { get; set; }

        /// <summary>
        /// Title of the schema
        /// </summary>
        [JsonPropertyName("title")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? title { get; set; }

        /// <summary>
        /// Default value for the property
        /// </summary>
        [JsonPropertyName("default")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? @default { get; set; }

        /// <summary>
        /// Minimum value (for numeric types)
        /// </summary>
        [JsonPropertyName("minimum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? minimum { get; set; }

        /// <summary>
        /// Maximum value (for numeric types)
        /// </summary>
        [JsonPropertyName("maximum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? maximum { get; set; }

        /// <summary>
        /// Minimum length (for string and array types)
        /// </summary>
        [JsonPropertyName("minLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? minLength { get; set; }

        /// <summary>
        /// Maximum length (for string and array types)
        /// </summary>
        [JsonPropertyName("maxLength")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? maxLength { get; set; }

        /// <summary>
        /// Pattern for string validation (regex)
        /// </summary>
        [JsonPropertyName("pattern")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? pattern { get; set; }

        /// <summary>
        /// Additional properties flag for objects
        /// </summary>
        [JsonPropertyName("additionalProperties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? additionalProperties { get; set; }
    }
}
