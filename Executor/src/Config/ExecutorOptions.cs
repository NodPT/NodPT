namespace BackendExecutor.Config;

/// <summary>
/// Configuration options for the executor service
/// </summary>
public class ExecutorOptions
{
    public const string SectionName = "Executor";

    /// <summary>
    /// Redis connection string (Legacy - prefer using Redis:ConnectionString in appsettings.json)
    /// </summary>
    public string RedisConnection { get; set; } = "localhost:6379";

    /// <summary>
    /// Maximum concurrent manager jobs (0 = unlimited)
    /// </summary>
    public int MaxManager { get; set; } = 0;

    /// <summary>
    /// Maximum concurrent inspector jobs (0 = unlimited)
    /// </summary>
    public int MaxInspector { get; set; } = 0;

    /// <summary>
    /// Maximum concurrent agent jobs (0 = unlimited)
    /// </summary>
    public int MaxAgent { get; set; } = 0;

    /// <summary>
    /// Maximum total concurrent jobs (0 = unlimited)
    /// </summary>
    public int MaxTotal { get; set; } = 0;

    /// <summary>
    /// LLM endpoint URL for chat completions
    /// </summary>
    public string LlmEndpoint { get; set; } = "http://ollama:11434/v1/chat/generate";

    /// <summary>
    /// Default model name to use for LLM chat completions
    /// </summary>
    public string DefaultModel { get; set; } = "deepseek-r1:1.5b";
}