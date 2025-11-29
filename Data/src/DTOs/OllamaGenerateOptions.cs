using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Options for Ollama generate API request
    /// </summary>
    public class OllamaGenerateOptions
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.3;

        [JsonPropertyName("num_predict")]
        public int NumPredict { get; set; } = 1000;
    }
}
