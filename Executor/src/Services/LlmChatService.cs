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
    /// Build OllamaOptions from AIModel properties
    /// </summary>
    public static OllamaOptions? BuildOptionsFromAIModel(AIModel? aiModel)
    {
        if (aiModel == null)
            return null;

        // Check if any option is set
        if (aiModel.Temperature == null && 
            aiModel.NumPredict == null && 
            aiModel.TopK == null && 
            aiModel.TopP == null && 
            aiModel.Seed == null && 
            aiModel.NumCtx == null && 
            aiModel.NumGpu == null && 
            aiModel.NumThread == null && 
            aiModel.RepeatPenalty == null && 
            string.IsNullOrEmpty(aiModel.Stop))
        {
            return null;
        }

        var options = new OllamaOptions
        {
            Temperature = aiModel.Temperature,
            NumPredict = aiModel.NumPredict,
            TopK = aiModel.TopK,
            TopP = aiModel.TopP,
            Seed = aiModel.Seed,
            NumCtx = aiModel.NumCtx,
            NumGpu = aiModel.NumGpu,
            NumThread = aiModel.NumThread,
            RepeatPenalty = aiModel.RepeatPenalty
        };

        // Parse stop sequences from comma-separated string
        if (!string.IsNullOrEmpty(aiModel.Stop))
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

        // Build options from AIModel if not already set
        if (request.options == null && aiModel != null)
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

            _logger.LogInformation("Sending chat request to LLM endpoint: {Endpoint}, Model: {Model}, Messages: {MessageCount}", 
                endpoint, request.model, request.messages?.Count ?? 0);

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
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
