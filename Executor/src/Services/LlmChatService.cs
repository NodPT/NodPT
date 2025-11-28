using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackendExecutor.Config;

namespace BackendExecutor.Services;

/// <summary>
/// Request model for Ollama API
/// </summary>
public class OllamaRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    /// <summary>
    /// User identifier (Firebase UID) for tracking
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }
}

/// <summary>
/// Message model for Ollama API
/// </summary>
public class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
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
            Messages = new List<OllamaMessage>
            {
                new OllamaMessage { Role = "user", Content = message }
            },
            Stream = false
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

            _logger.LogInformation("Sending chat request to LLM endpoint: {Endpoint}, Model: {Model}, Messages: {MessageCount}", 
                _options.LlmEndpoint, request.Model, request.Messages.Count);

            var response = await _httpClient.PostAsync(_options.LlmEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            if (responseObject?.Message == null)
            {
                _logger.LogWarning("LLM response has no message");
                return string.Empty;
            }

            var result = responseObject.Message.Content ?? string.Empty;
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

    // Response DTOs matching Ollama API format
    private class OllamaResponse
    {
        [JsonPropertyName("message")]
        public OllamaResponseMessage? Message { get; set; }
    }

    private class OllamaResponseMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
