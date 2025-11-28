using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackendExecutor.Config;

namespace BackendExecutor.Services;

/// <summary>
/// Request model for Ollama API (generate endpoint)
/// </summary>
public class OllamaRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

/// <summary>
/// Options for Ollama API request
/// </summary>
public class OllamaOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0;
}

/// <summary>
/// Service for sending chat messages to LLM endpoint
/// </summary>
public interface ILlmChatService
{
    /// <summary>
    /// Send a chat message to the LLM endpoint
    /// </summary>
    /// <param name="message">The message content</param>
    /// <param name="model">The model to use</param>
    /// <param name="maxTokens">Maximum tokens in response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LLM response content</returns>
    Task<string> SendChatMessageAsync(string message, string model, int maxTokens = 64, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a chat message object to the LLM endpoint (object will be serialized)
    /// </summary>
    /// <param name="messageObject">The message object to serialize</param>
    /// <param name="model">The model to use</param>
    /// <param name="maxTokens">Maximum tokens in response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LLM response content</returns>
    Task<string> SendChatMessageAsync(object messageObject, string model, int maxTokens = 64, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a structured Ollama request to the LLM endpoint
    /// </summary>
    /// <param name="request">The Ollama request object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LLM response content</returns>
    Task<string> SendChatRequestAsync(OllamaRequest request, CancellationToken cancellationToken = default);
}

public class LlmChatService : ILlmChatService
{
    private readonly HttpClient _httpClient;
    private readonly ExecutorOptions _options;
    private readonly ILogger<LlmChatService> _logger;

    public LlmChatService(
        HttpClient httpClient,
        ExecutorOptions options,
        ILogger<LlmChatService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message to the LLM endpoint
    /// </summary>
    public async Task<string> SendChatMessageAsync(
        string message,
        string model,
        int maxTokens = 64,
        CancellationToken cancellationToken = default)
    {
        var request = new OllamaRequest
        {
            Model = model,
            Prompt = message,
            Stream = false,
            Options = new OllamaOptions { Temperature = 0 }
        };

        return await SendChatRequestAsync(request, cancellationToken);
    }

    /// <summary>
    /// Send a chat message object to the LLM endpoint (object will be serialized)
    /// </summary>
    public async Task<string> SendChatMessageAsync(
        object messageObject,
        string model,
        int maxTokens = 64,
        CancellationToken cancellationToken = default)
    {
        var message = JsonSerializer.Serialize(messageObject);
        return await SendChatMessageAsync(message, model, maxTokens, cancellationToken);
    }

    /// <summary>
    /// Send a structured Ollama request to the LLM endpoint
    /// </summary>
    public async Task<string> SendChatRequestAsync(
        OllamaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending chat request to LLM endpoint: {Endpoint}, Model: {Model}", 
                _options.LlmEndpoint, request.Model);

            var response = await _httpClient.PostAsync(_options.LlmEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            if (responseObject == null)
            {
                _logger.LogWarning("LLM response is null");
                return string.Empty;
            }

            var result = responseObject.Response ?? string.Empty;
            _logger.LogInformation("Received LLM response with {Length} characters", result.Length);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling LLM endpoint: {Endpoint}", _options.LlmEndpoint);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error while processing LLM request/response");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling LLM endpoint");
            throw;
        }
    }

    // Response DTO matching Ollama API format
    private class OllamaResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("done_reason")]
        public string? DoneReason { get; set; }

        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }

        [JsonPropertyName("load_duration")]
        public long LoadDuration { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }

        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }
    }
}
