using BackendExecutor.Config;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace BackendExecutor.Services;


public class LlmChatService
{
    private readonly OllamaLlmClient _ollamaClient;
    private readonly TensorRtChatClient _tensorRtClient;
    private readonly ExecutorOptions _options;
    private readonly ILogger<LlmChatService> _logger;

    public LlmChatService(
        OllamaLlmClient ollamaClient,
        TensorRtChatClient tensorRtClient,
        ExecutorOptions options,
        ILogger<LlmChatService> logger)
    {
        _ollamaClient = ollamaClient;
        _tensorRtClient = tensorRtClient;
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
        NodPT.Data.DTOs.OllamaRequest request,
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
        NodPT.Data.DTOs.OllamaRequest request,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("LLM endpoint is required.", nameof(endpoint));

        // Choose backend client.
        // - TensorRT: user-defined /api/chat endpoint (plus TensorRT-only fields like response_format/tools)
        // - Ollama: everything else (OllamaSharp handles /api/generate and /api/chat)
        if (_tensorRtClient.CanHandle(endpoint, request))
        {
            _logger.LogInformation("Routing LLM request to TensorRT chat backend. Endpoint={Endpoint}", endpoint);
            return await _tensorRtClient.SendAsync(request, endpoint, cancellationToken);
        }

        _logger.LogInformation("Routing LLM request to Ollama backend. Endpoint={Endpoint}", endpoint);
        return await _ollamaClient.SendAsync(request, endpoint, cancellationToken);
    }
}
