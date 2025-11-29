namespace NodPT.Data.DTOs
{

    /// <summary>
    /// Configuration options for the summarization service.
    /// </summary>
    public class SummarizationOptions
    {
        /// <summary>
        /// Base URL for the Ollama endpoint used specifically for summarization.
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:11434/api/generate";

        /// <summary>
        /// Model name for the summarization model.
        /// </summary>
        public string Model { get; set; } = "llama3.2:1b";

        /// <summary>
        /// Timeout in seconds for summarization requests.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Maximum length of the summary in characters.
        /// </summary>
        public int MaxSummaryLength { get; set; } = 2000;
    }
}
