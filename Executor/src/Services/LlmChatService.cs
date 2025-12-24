using BackendExecutor.Config;
using NodPT.Data.DTOs;
using NodPT.Data.Models;
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
    /// Build OllamaOptions from AIModel properties with optimistic defaults
    /// </summary>
    public static OllamaOptions BuildOptionsFromAIModel(AIModel? aiModel)
    {
        // Default optimistic values for Ollama options
        const double DefaultTemperature = 0.7;      // Balanced creativity and coherence
        const int DefaultNumPredict = 2048;         // Reasonable response length
        const int DefaultTopK = 40;                 // Common default for top-k sampling
        const double DefaultTopP = 0.9;             // Nucleus sampling default
        const int DefaultNumCtx = 4096;             // Good context window size
        const double DefaultRepeatPenalty = 1.1;    // Slight penalty to reduce repetition

        var options = new OllamaOptions
        {
            Temperature = aiModel?.Temperature ?? DefaultTemperature,
            NumPredict = aiModel?.NumPredict ?? DefaultNumPredict,
            TopK = aiModel?.TopK ?? DefaultTopK,
            TopP = aiModel?.TopP ?? DefaultTopP,
            NumCtx = aiModel?.NumCtx ?? DefaultNumCtx,
            RepeatPenalty = aiModel?.RepeatPenalty ?? DefaultRepeatPenalty,
            // These don't have universal defaults - use AIModel values if set
            Seed = aiModel?.Seed,
            NumGpu = aiModel?.NumGpu,
            NumThread = aiModel?.NumThread
        };

        // Parse stop sequences from comma-separated string
        if (!string.IsNullOrEmpty(aiModel?.Stop))
        {
            options.Stop = aiModel.Stop
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        return options;
    }

    /// <summary>
    /// Send a structured Ollama request to the LLM endpoint using AIModel settings
    /// </summary>
    public async Task<string> SendChatRequestAsync(
        OllamaRequest request,
        AIModel? aiModel,
        CancellationToken cancellationToken = default)
    {
        // Use endpoint from AIModel if available, otherwise use default
        var endpoint = !string.IsNullOrEmpty(aiModel?.EndpointAddress) 
            ? aiModel.EndpointAddress 
            : _options.LlmEndpoint;

        // Build options from AIModel if not already set (always returns optimistic defaults)
        if (request.options == null)
        {
            request.options = BuildOptionsFromAIModel(aiModel);
        }

        return await SendChatRequestAsync(request, endpoint, cancellationToken);
    }

    /// <summary>
    /// Send a structured Ollama request to the LLM endpoint (uses default endpoint from config)
    /// </summary>
    public async Task<string> SendChatRequestAsync(
        OllamaRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendChatRequestAsync(request, _options.LlmEndpoint, cancellationToken);
    }

    /// <summary>
    /// Send a structured Ollama request to a specific endpoint
    /// </summary>
    public async Task<string> SendChatRequestAsync(
        OllamaRequest request,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("=== Sending Chat Request to LLM ===");
            _logger.LogInformation("Endpoint: {Endpoint}", endpoint);
            _logger.LogInformation("Model: {Model}", request.model);
            _logger.LogInformation("Message Count: {MessageCount}", request.messages?.Count ?? 0);
            
            // Log request payload (truncate if too long)
            if (json.Length <= 2000)
            {
                _logger.LogInformation("Request Payload: {RequestPayload}", json);
            }
            else
            {
                _logger.LogInformation("Request Payload (first 2000 chars): {RequestPayload}", json.Substring(0, 2000));
                _logger.LogInformation("Request Payload Total Length: {PayloadLength} chars", json.Length);
            }

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Log response payload (truncate if too long)
            if (responseJson.Length <= 2000)
            {
                _logger.LogInformation("Response Payload: {ResponsePayload}", responseJson);
            }
            else
            {
                _logger.LogInformation("Response Payload (first 2000 chars): {ResponsePayload}", responseJson.Substring(0, 2000));
                _logger.LogInformation("Response Payload Total Length: {PayloadLength} chars", responseJson.Length);
            }
            
            var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            if (responseObject == null)
            {
                _logger.LogWarning("LLM response is null");
                return string.Empty;
            }

            var result = responseObject.response ?? string.Empty;
            _logger.LogInformation("=== LLM Response Processed ===");
            _logger.LogInformation("Response Content Length: {Length} characters", result.Length);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling LLM endpoint: {Endpoint}", endpoint);
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
