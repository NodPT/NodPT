using System.Text;
using System.Text.Json;
using BackendExecutor.Config;

namespace BackendExecutor.Services;

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
        try
        {
            var requestBody = new
            {
                model = model,
                messages = new[] { new { role = "user", content = message } },
                max_tokens = maxTokens
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending chat message to LLM endpoint: {Endpoint}, Model: {Model}", _options.LlmEndpoint, model);

            var response = await _httpClient.PostAsync(_options.LlmEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<LlmResponse>(responseJson);

            if (responseObject?.Choices == null || responseObject.Choices.Length == 0)
            {
                _logger.LogWarning("LLM response has no choices");
                return string.Empty;
            }

            var result = responseObject.Choices[0].Message?.Content ?? string.Empty;
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

    // Response DTOs matching LLM API format
    private class LlmResponse
    {
        public Choice[]? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }
}
