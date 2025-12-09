using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Options for Ollama generate API request
    /// </summary>
    public class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        [JsonPropertyName("num_predict")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumPredict { get; set; }

        [JsonPropertyName("top_k")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TopK { get; set; }

        [JsonPropertyName("top_p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? TopP { get; set; }

        [JsonPropertyName("seed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Seed { get; set; }

        [JsonPropertyName("num_ctx")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumCtx { get; set; }

        [JsonPropertyName("num_gpu")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumGpu { get; set; }

        [JsonPropertyName("num_thread")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumThread { get; set; }

        [JsonPropertyName("repeat_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? RepeatPenalty { get; set; }

        [JsonPropertyName("stop")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Stop { get; set; }

        /// <summary>
        /// Frequency penalty for TensorRT-LLM (OpenAI-style)
        /// Penalizes tokens based on their frequency in the text so far
        /// Default: 0.0
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? frequency_penalty { get; set; }

        /// <summary>
        /// Presence penalty for TensorRT-LLM (OpenAI-style)
        /// Penalizes tokens that have already appeared in the text
        /// Default: 0.0
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? presence_penalty { get; set; }

        /// <summary>
        /// Whether to return log probabilities for TensorRT-LLM
        /// </summary>
        [JsonPropertyName("logprobs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? logprobs { get; set; }

        /// <summary>
        /// Metadata object for TensorRT-LLM pipeline
        /// Transparent passthrough for additional values
        /// </summary>
        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? metadata { get; set; }
    }
}
