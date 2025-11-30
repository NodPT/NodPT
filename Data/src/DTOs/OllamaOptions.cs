using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Options for Ollama generate API request
    /// </summary>
    public class OllamaOptions
    {
        public double temperature { get; set; } = 0.3;

        public int numPredict { get; set; } = 1000;
    }
}
