namespace BackendExecutor.Config;

/// <summary>
/// Configuration options for the summarization service.
/// Used to call Ollama's summarizer model for rolling memory updates.
/// </summary>
public class SummarizationOptions
{
    public const string SectionName = "Summarization";

    /// <summary>
    /// Base URL for the Ollama endpoint used specifically for summarization.
    /// Should point to /api/generate endpoint.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/api/generate";

    /// <summary>
    /// Model name for the summarization model.
    /// Should be a smaller, faster model optimized for summarization.
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
