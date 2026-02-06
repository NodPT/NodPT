using NodPT.Data.DTOs;
using System.Text;
using System.Text.Json;

namespace BackendExecutor.Services;

public class TensorRtChatClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TensorRtChatClient> _logger;

    public TensorRtChatClient(IHttpClientFactory httpClientFactory, ILogger<TensorRtChatClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool CanHandle(string endpoint, OllamaRequest request)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return false;

        // User requirement: TensorRT is exposed as /api/chat.
        if (!uri.AbsolutePath.TrimEnd('/').Equals("/api/generate", StringComparison.OrdinalIgnoreCase))
            return false;

        // Heuristic:
        // - If it isn't the canonical Ollama port/host, treat it as TensorRT.
        // - Or if TensorRT-only fields are present, treat it as TensorRT even if path collides.
        var hasTensorRtFields = request.response_format != null || request.tool_choice != null || (request.tools?.Count > 0);

        if (hasTensorRtFields)
            return true;

        if (uri.Port != 11434 && !uri.Host.Contains("ollama", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public async Task<string> SendAsync(OllamaRequest request, string endpoint, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("LlmClient");

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Using TensorRT /api/chat (raw HTTP). Endpoint={Endpoint} Model={Model} Messages={Count}",
            endpoint,
            request.model,
            request.messages?.Count ?? 0);

        using var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var requestUri = response.RequestMessage?.RequestUri?.ToString() ?? endpoint;

            if (errorBody.Length <= 2000)
            {
                _logger.LogError(
                    "TensorRT chat call failed. Status={StatusCode}. RequestUri={RequestUri}. Body={Body}",
                    (int)response.StatusCode, requestUri, errorBody);
            }
            else
            {
                _logger.LogError(
                    "TensorRT chat call failed. Status={StatusCode}. RequestUri={RequestUri}. Body(first 2000)={Body}",
                    (int)response.StatusCode, requestUri, errorBody.Substring(0, 2000));
            }
        }

        // response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        // Reuse existing response DTO: it already supports both response and message.content.
        var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseJson);
        return responseObject?.Content ?? string.Empty;
    }
}
