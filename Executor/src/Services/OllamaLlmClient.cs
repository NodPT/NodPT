using BackendExecutor.Config;
using NodPT.Data.DTOs;
using OllamaSharp;
using OllamaSharp.AsyncEnumerableExtensions;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;

namespace BackendExecutor.Services;

public class OllamaLlmClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExecutorOptions _options;
    private readonly ILogger<OllamaLlmClient> _logger;

    public OllamaLlmClient(IHttpClientFactory httpClientFactory, ExecutorOptions options, ILogger<OllamaLlmClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<string> SendAsync(NodPT.Data.DTOs.OllamaRequest request, string endpoint, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("LLM endpoint is required.", nameof(endpoint));

        // OllamaSharp expects BaseAddress = http(s)://host:port and it appends api/* internally.
        // We keep honoring the configured full endpoint to decide between generate vs chat.
        var baseUri = GetBaseUri(endpoint);

        var model = !string.IsNullOrWhiteSpace(request.model) ? request.model : _options.DefaultModel;

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = baseUri;

        var ollama = new OllamaApiClient(httpClient, defaultModel: model)
        {
            SelectedModel = model
        };

        if (IsOllamaGenerateEndpoint(endpoint))
        {
            var prompt = request.prompt;
            if (string.IsNullOrEmpty(prompt) && request.messages is { Count: > 0 })
                prompt = BuildPromptFromMessages(request.messages);

            var generateRequest = new GenerateRequest
            {
                Model = model,
                Prompt = prompt ?? string.Empty,
                Stream = true,
                Options = MapOptions(request.options),
                Images = request.images?.ToArray()
            };

            _logger.LogInformation("Using Ollama generate via OllamaSharp. BaseUri={BaseUri} Model={Model}", baseUri, model);

            var done = await ollama.GenerateAsync(generateRequest, cancellationToken).StreamToEndAsync();
            return done?.Response ?? string.Empty;
        }

        // Default to /api/chat semantics when messages are provided.
        if (request.messages is { Count: > 0 })
        {
            var chatRequest = new ChatRequest
            {
                Model = model,
                Messages = request.messages.Select(ToOllamaSharpMessage).ToList(),
                Stream = true,
                Options = MapOptions(request.options)
            };

            _logger.LogInformation("Using Ollama chat via OllamaSharp. BaseUri={BaseUri} Model={Model} Messages={Count}", baseUri, model, request.messages.Count);

            var done = await ollama.ChatAsync(chatRequest, cancellationToken).StreamToEndAsync();
            return done?.Message?.Content ?? string.Empty;
        }

        // No messages: treat it as generate with prompt.
        var fallbackGenerate = new GenerateRequest
        {
            Model = model,
            Prompt = request.prompt ?? string.Empty,
            Stream = true,
            Options = MapOptions(request.options),
            Images = request.images?.ToArray()
        };

        _logger.LogInformation("Using Ollama generate (fallback) via OllamaSharp. BaseUri={BaseUri} Model={Model}", baseUri, model);

        var fallbackDone = await ollama.GenerateAsync(fallbackGenerate, cancellationToken).StreamToEndAsync();
        return fallbackDone?.Response ?? string.Empty;
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

    private static bool IsOllamaGenerateEndpoint(string endpoint)
        => endpoint.TrimEnd('/').EndsWith("/api/generate", StringComparison.OrdinalIgnoreCase);

    private static Uri GetBaseUri(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid LLM endpoint URL: {endpoint}", nameof(endpoint));

        return new UriBuilder(uri.Scheme, uri.Host, uri.IsDefaultPort ? -1 : uri.Port).Uri;
    }

    private static OllamaSharp.Models.Chat.Message ToOllamaSharpMessage(OllamaMessage message)
    {
        var role = (message.role ?? string.Empty).Trim().ToLowerInvariant();
        var content = message.content ?? string.Empty;

        return role switch
        {
            "system" => new OllamaSharp.Models.Chat.Message(ChatRole.System, content),
            "assistant" => new OllamaSharp.Models.Chat.Message(ChatRole.Assistant, content),
            _ => new OllamaSharp.Models.Chat.Message(ChatRole.User, content)
        };
    }

    private static RequestOptions? MapOptions(OllamaOptions? options)
    {
        if (options == null)
            return null;

        return new RequestOptions
        {
            Temperature = options.Temperature is null ? null : Convert.ToSingle(options.Temperature.Value),
            NumPredict = options.NumPredict,
            TopK = options.TopK,
            TopP = options.TopP is null ? null : Convert.ToSingle(options.TopP.Value),
            Seed = options.Seed,
            NumCtx = options.NumCtx,
            NumGpu = options.NumGpu,
            NumThread = options.NumThread,
            RepeatPenalty = options.RepeatPenalty is null ? null : Convert.ToSingle(options.RepeatPenalty.Value),
            Stop = options.Stop?.ToArray(),
            FrequencyPenalty = options.frequency_penalty is null ? null : Convert.ToSingle(options.frequency_penalty.Value),
            PresencePenalty = options.presence_penalty is null ? null : Convert.ToSingle(options.presence_penalty.Value),
        };
    }

    private static string BuildPromptFromMessages(List<OllamaMessage> messages)
    {
        var sb = new StringBuilder();
        foreach (var message in messages)
        {
            var role = string.IsNullOrWhiteSpace(message.role) ? "user" : message.role.Trim();
            var content = message.content ?? string.Empty;

            sb.Append(role.ToUpperInvariant());
            sb.Append(": ");
            sb.AppendLine(content);
            sb.AppendLine();
        }

        sb.Append("ASSISTANT: ");
        return sb.ToString();
    }
}
