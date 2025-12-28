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



    /// Build OllamaOptions from AIModel properties with optimistic defaults
    /// If aiModel is null, returns options with default values suitable for most use cases
    /// </summary>
    /// <param name="aiModel">The AIModel to extract options from, or null to use all defaults</param>
    /// <returns>OllamaOptions with either AIModel values or optimistic defaults</returns>
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
    /// Send a structured Ollama request to a specific endpoint
    /// </summary>
    public async Task<string> SendChatRequestAsync(
        OllamaRequest request,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("LLM endpoint is required.", nameof(endpoint));
            }

            // IMPORTANT:
            // - /api/generate expects { model, prompt, ... }
            // - /api/chat expects { model, messages, ... }
            // The current executor flow builds messages[]. If you're targeting /api/generate (your curl case),
            // we convert messages[] into a single prompt to avoid endpoint/payload mismatch.
            if (IsOllamaGenerateEndpoint(endpoint) && request.prompt == null && request.messages is { Count: > 0 })
            {
                request.prompt = BuildPromptFromMessages(request.messages);
                request.messages = null;
            }

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("=== Sending Chat Request to LLM ===");
            _logger.LogInformation("Endpoint: {Endpoint}", endpoint);
            _logger.LogInformation("Model: {Model}", request.model);
            _logger.LogInformation("Message Count: {MessageCount}", request.messages?.Count ?? 0);

            // Log request payload only at Debug level to avoid exposing sensitive user data
            if (json.Length <= 2000)
            {
                _logger.LogInformation("Request Payload: {RequestPayload}", json);
            }
            else
            {
                _logger.LogInformation("Request Payload (first 2000 chars): {RequestPayload}", json.Substring(0, 2000));
                _logger.LogInformation("Request Payload Total Length: {PayloadLength} chars", json.Length);
            }


            //! Send request to LLM endpoint
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var requestUri = response.RequestMessage?.RequestUri?.ToString() ?? endpoint;

                if (errorBody.Length <= 2000)
                {
                    _logger.LogError(
                        "LLM call failed. Status={StatusCode}. RequestUri={RequestUri}. Body={Body}",
                        (int)response.StatusCode, requestUri, errorBody);
                }
                else
                {
                    _logger.LogError(
                        "LLM call failed. Status={StatusCode}. RequestUri={RequestUri}. Body(first 2000)={Body}",
                        (int)response.StatusCode, requestUri, errorBody.Substring(0, 2000));
                }
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            // Log response payload only at Debug level to avoid exposing sensitive content
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

            var result = responseObject.Content;
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

    private static bool IsOllamaGenerateEndpoint(string endpoint)
    {
        // Treat anything ending with /api/generate as Ollama generate endpoint
        // (works for http://ollama:11434/api/generate and similar).
        return endpoint.TrimEnd('/').EndsWith("/api/generate", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildPromptFromMessages(List<OllamaMessage> messages)
    {
        var sb = new StringBuilder();
        foreach (var message in messages)
        {
            var role = string.IsNullOrWhiteSpace(message.role) ? "user" : message.role.Trim();
            var content = message.content ?? string.Empty;

            // Minimal, readable transcript format.
            sb.Append(role.ToUpperInvariant());
            sb.Append(": ");
            sb.AppendLine(content);
            sb.AppendLine();
        }

        // Nudge the model to answer as assistant.
        sb.Append("ASSISTANT: ");
        return sb.ToString();
    }


}
