using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace NodPT.Data.Services;

/// <summary>
/// Configuration options for the summarization service.
/// </summary>
public class SummarizationOptions
{
    /// <summary>
    /// Base URL for the Ollama endpoint used specifically for summarization.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/api/generate";

    /// <summary>
    /// Model name for the summarization model.
    /// </summary>
    public string Model { get; set; } = "llama3.2:1b";

    /// <summary>
    /// Timeout in seconds for summarization requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum length of the summary in characters.
    /// </summary>
    public int MaxSummaryLength { get; set; } = 2000;
}

/// <summary>
/// Request model for Ollama generate API
/// </summary>
public class OllamaGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("options")]
    public OllamaGenerateOptions? Options { get; set; }
}

/// <summary>
/// Options for Ollama generate API request
/// </summary>
public class OllamaGenerateOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.3;

    [JsonPropertyName("num_predict")]
    public int NumPredict { get; set; } = 1000;
}

/// <summary>
/// Response model for Ollama generate API
/// </summary>
public class OllamaGenerateResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

/// <summary>
/// Service for calling Ollama summarizer endpoint to perform rolling summarization.
/// This service is responsible ONLY for the actual summarization request.
/// It does not touch Redis or the database.
/// </summary>
public interface ISummarizationService
{
    /// <summary>
    /// Summarize an old summary with a new message to produce an updated summary.
    /// </summary>
    /// <param name="oldSummary">The existing summary text</param>
    /// <param name="newMessageContent">The new message to integrate</param>
    /// <param name="role">The role of the message sender: "user" or "ai_assistant"</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new merged summary text</returns>
    Task<string> SummarizeAsync(string oldSummary, string newMessageContent, string role, CancellationToken cancellationToken = default);
}

public class SummarizationService : ISummarizationService
{
    private readonly HttpClient _httpClient;
    private readonly SummarizationOptions _options;
    private readonly ILogger<SummarizationService> _logger;

    public SummarizationService(
        HttpClient httpClient,
        SummarizationOptions options,
        ILogger<SummarizationService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    /// <summary>
    /// Summarize an old summary with a new message to produce an updated summary.
    /// </summary>
    public async Task<string> SummarizeAsync(
        string oldSummary,
        string newMessageContent,
        string role,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build the summarization prompt based on role
            var prompt = BuildSummarizationPrompt(oldSummary, newMessageContent, role);

            var request = new OllamaGenerateRequest
            {
                Model = _options.Model,
                Prompt = prompt,
                Stream = false,
                Options = new OllamaGenerateOptions
                {
                    Temperature = 0.3,
                    NumPredict = _options.MaxSummaryLength / 4 // Approximate tokens
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending summarization request to {Endpoint}, Model: {Model}, Role: {Role}",
                _options.BaseUrl, _options.Model, role);

            var response = await _httpClient.PostAsync(_options.BaseUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson);

            if (responseObject == null || string.IsNullOrEmpty(responseObject.Response))
            {
                _logger.LogWarning("Summarization response is empty, falling back to old summary");
                return oldSummary;
            }

            var newSummary = responseObject.Response.Trim();

            // Enforce max length
            if (newSummary.Length > _options.MaxSummaryLength)
            {
                newSummary = newSummary.Substring(0, _options.MaxSummaryLength);
            }

            _logger.LogInformation("Summarization completed: {OldLength} chars + {NewMsgLength} chars -> {NewLength} chars",
                oldSummary.Length, newMessageContent.Length, newSummary.Length);

            return newSummary;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during summarization, falling back to old summary");
            return oldSummary; // Graceful fallback
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Summarization request timed out, falling back to old summary");
            return oldSummary; // Graceful fallback
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during summarization, falling back to old summary");
            return oldSummary; // Graceful fallback
        }
    }

    /// <summary>
    /// Build the summarization prompt based on the role of the message.
    /// </summary>
    private string BuildSummarizationPrompt(string oldSummary, string newMessageContent, string role)
    {
        var roleInstructions = role.ToLowerInvariant() switch
        {
            "user" => @"Focus on integrating from the new user message:
- New goals, tasks, and questions
- New constraints (time, budget, technology)
- New preferences (style, tone, priorities)
- New contextual facts provided by the user
Non-essential chatter can be compressed as long as meaning is preserved.",

            "ai_assistant" or "assistant" => @"Focus on integrating from the AI assistant message:
- Final answers and solutions provided
- Frameworks or plans laid out
- Decisions or commitments made
- Clarifications or interpretations that affect future reasoning
The actual wording is not important; extract the key decisions and information.",

            _ => @"Integrate the key information from the new message into the summary.
Preserve all important facts, constraints, and decisions."
        };

        var prompt = $@"You are a precise summarization assistant. Your task is to merge an existing conversation summary with a new message to create an updated summary.

EXISTING SUMMARY:
{(string.IsNullOrEmpty(oldSummary) ? "(No previous summary)" : oldSummary)}

NEW MESSAGE (from {role}):
{newMessageContent}

INSTRUCTIONS:
{roleInstructions}

RULES:
1. Preserve all important facts, constraints, preferences, instructions, and decisions from the existing summary.
2. Integrate relevant new information from the new message.
3. Do not invent or assume information not present in the inputs.
4. Output ONLY the updated summary text, no explanations or meta-commentary.
5. Keep the summary concise but complete.
6. Use clear, factual language.

UPDATED SUMMARY:";

        return prompt;
    }
}
