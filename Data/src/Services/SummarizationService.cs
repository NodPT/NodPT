using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NodPT.Data.DTOs;
using NodPT.Data.Interfaces;

namespace NodPT.Data.Services;

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
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

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
