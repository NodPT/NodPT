using BackendExecutor.Config;
using NodPT.Data.DTOs;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendExecutor.Services;


public class LlmChatService 
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
            model = model,
            messages = new List<OllamaMessage>
            {
                new OllamaMessage { role = "user", content = message }
            },
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
                _options.LlmEndpoint, request.model, request.messages.Count);

            var response = await _httpClient.PostAsync(_options.LlmEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            if (responseObject == null)
            {
                _logger.LogWarning("LLM response is null");
                return string.Empty;
            }

            var result = responseObject.response ?? string.Empty;
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

   
}
