using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevExpress.Xpo;
using Microsoft.Extensions.Logging;
using NodPT.Data.DTOs;
using NodPT.Data.Models;

namespace NodPT.Data.Services;

public class SummarizationService
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

            var request = new OllamaRequest
            {
                model = _options.Model,
                prompt = prompt,
            };

            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            _logger.LogDebug("Sending summarization request to {Endpoint}, Model: {Model}, Role: {Role}",
             _options.BaseUrl, _options.Model, role);
            // Send the request
            var response = await _httpClient.PostAsync(_options.BaseUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            // Parse the response
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseObject = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            if (responseObject == null || string.IsNullOrEmpty(responseObject.Content))
            {
                _logger.LogWarning("Summarization response is empty, falling back to old summary");
                return oldSummary;
            }
            // Extract the new summary
            var newSummary = responseObject.Content.Trim();

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

        UnitOfWork? session = DatabaseHelper.GetSession();
        if (session == null)
            return string.Empty;

        // get the role instructions
        IQueryable<SummarizePrompts> data = session.Query<SummarizePrompts>()
            .Where(x => x.Role.ToLower() == role.ToLower());

        var prompt = string.IsNullOrEmpty(oldSummary) ? string.Empty : $"EXISTING SUMMARY: {oldSummary}";
        prompt += $"NEW MESSAGE (from {role}): {newMessageContent}" +
                $"INSTRUCTIONS:";

        if (data.Any())
        {
            foreach (var item in data)
            {
                prompt += $" {item.Prompt}. ";
            }
        }
        else
        {
            prompt += " Integrate the key information from the new message into the summary. " +
                "Preserve all important facts, constraints, and decisions.";


            prompt += role.ToLowerInvariant() switch
            {
                "user" => @"Focus on integrating from the new user message:
    - New goals, tasks, and questions
    - New constraints (time, budget, technology)
    - New preferences (style, tone, priorities)
    - New contextual facts provided by the user
    Non-essential chatter can be compressed as long as meaning is preserved.",

                "assistant" => @"Focus on integrating from the AI assistant message:
    - Final answers and solutions provided
    - Frameworks or plans laid out
    - Decisions or commitments made
    - Clarifications or interpretations that affect future reasoning
    The actual wording is not important; extract the key decisions and information.",

                _ => @"Integrate the key information from the new message into the summary.
    Preserve all important facts, constraints, and decisions."
            };

            prompt += " RULES: " +
                   "Preserve all important facts, constraints, preferences, instructions, and decisions from the existing summary.\r\n2. " +
                   "Integrate relevant new information from the new message.\r\n3. Do not invent or assume information not present in the inputs.\r\n4. " +
                   "Output ONLY the updated summary text, no explanations or meta-commentary.\r\n5. Keep the summary concise but complete.\r\n6. " +
                   "Use clear, factual language.";
        }

        return prompt;
    }
}
