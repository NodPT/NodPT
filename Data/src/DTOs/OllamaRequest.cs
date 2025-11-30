using System.Text.Json.Serialization;

namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Request model for Ollama generate API
    /// </summary>
    public class OllamaRequest
    {
        public string model { get; set; } = string.Empty;

        public string prompt { get; set; } = string.Empty;

        public bool stream { get; private set; } = false;

        public OllamaOptions? options { get; set; }
        public List<OllamaMessage> messages { get; set; }
        public List<string> images { get;set; }
    }
}
