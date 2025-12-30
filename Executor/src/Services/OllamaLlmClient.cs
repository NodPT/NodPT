using BackendExecutor.Config;
using NodPT.Data.DTOs;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendExecutor.Services;

public class OllamaLlmClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExecutorOptions _options;
    private readonly ILogger<OllamaLlmClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaLlmClient(IHttpClientFactory httpClientFactory, ExecutorOptions options, ILogger<OllamaLlmClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<string> SendAsync(OllamaRequest request, string endpoint, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("LLM endpoint is required.", nameof(endpoint));

        var model = !string.IsNullOrWhiteSpace(request.model) ? request.model : _options.DefaultModel;
        var httpClient = _httpClientFactory.CreateClient();

        // Determine if we should use /api/chat or /api/generate
        var baseUri = GetBaseUri(endpoint);
        string requestUri;
        object requestBody;

        if (request.messages is { Count: > 0 })
        {
            // Use chat endpoint
            requestUri = new Uri(baseUri, "/api/chat").ToString();
            requestBody = new
            {
                model,
                messages = request.messages.Select(m => new { role = m.role ?? "user", content = m.content ?? "" }).ToList(),
                stream = false,
                options = MapOptions(request.options)
            };

            _logger.LogInformation("Sending Ollama chat request. Endpoint={Endpoint} Model={Model} Messages={Count}", requestUri, model, request.messages.Count);
        }
        else
        {
            // Use generate endpoint
            requestUri = new Uri(baseUri, "/api/generate").ToString();
            requestBody = new
            {
                model,
                prompt = request.prompt ?? string.Empty,
                stream = false,
                options = MapOptions(request.options),
                images = request.images
            };

            _logger.LogInformation("Sending Ollama generate request. Endpoint={Endpoint} Model={Model}", requestUri, model);
        }

        var jsonContent = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(requestUri, content, cancellationToken);
        // response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        // Parse response based on endpoint type
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        // Chat response has message.content, generate response has response
        if (root.TryGetProperty("message", out var messageElement) &&
            messageElement.TryGetProperty("content", out var contentElement))
        {
            return contentElement.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("response", out var responseElement))
        {
            return responseElement.GetString() ?? string.Empty;
        }

        _logger.LogWarning("Unexpected Ollama response format: {Response}", responseJson);
        return string.Empty;
    }

    public static bool LooksLikeOllamaEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return false;

        // By convention, Ollama runs on 11434 and/or uses host "ollama" in docker.
        if (uri.Port == 11434)
            return true;

        if (uri.Host.Contains("ollama", StringComparison.OrdinalIgnoreCase))
            return true;

        // Also accept explicit native paths.
        if (uri.AbsolutePath.Contains("/api/", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static Uri GetBaseUri(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid LLM endpoint URL: {endpoint}", nameof(endpoint));

        return new UriBuilder(uri.Scheme, uri.Host, uri.IsDefaultPort ? -1 : uri.Port).Uri;
    }

    private static Dictionary<string, object>? MapOptions(OllamaOptions? options)
    {
        if (options == null)
            return null;

        var dict = new Dictionary<string, object>();

        if (options.Temperature.HasValue) dict["temperature"] = options.Temperature.Value;
        if (options.NumPredict.HasValue) dict["num_predict"] = options.NumPredict.Value;
        if (options.TopK.HasValue) dict["top_k"] = options.TopK.Value;
        if (options.TopP.HasValue) dict["top_p"] = options.TopP.Value;
        if (options.Seed.HasValue) dict["seed"] = options.Seed.Value;
        if (options.NumCtx.HasValue) dict["num_ctx"] = options.NumCtx.Value;
        if (options.NumGpu.HasValue) dict["num_gpu"] = options.NumGpu.Value;
        if (options.NumThread.HasValue) dict["num_thread"] = options.NumThread.Value;
        if (options.RepeatPenalty.HasValue) dict["repeat_penalty"] = options.RepeatPenalty.Value;
        if (options.Stop != null && options.Stop.Count > 0) dict["stop"] = options.Stop;
        if (options.frequency_penalty.HasValue) dict["frequency_penalty"] = options.frequency_penalty.Value;
        if (options.presence_penalty.HasValue) dict["presence_penalty"] = options.presence_penalty.Value;

        return dict.Count > 0 ? dict : null;
    }
}
