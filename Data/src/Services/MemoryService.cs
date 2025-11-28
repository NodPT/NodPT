using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using Microsoft.Extensions.Logging;
using NodPT.Data.Models;
using System.Text.Json;

namespace NodPT.Data.Services;

/// <summary>
/// Configuration options for the memory service.
/// </summary>
public class MemoryOptions
{
    /// <summary>
    /// Maximum number of recent messages to keep in Redis history.
    /// </summary>
    public int HistoryLimit { get; set; } = 3;

    /// <summary>
    /// Redis key prefix for storing node summaries.
    /// Keys will be formatted as: {prefix}:{nodeId}
    /// </summary>
    public string SummaryKeyPrefix { get; set; } = "summary";

    /// <summary>
    /// Redis key prefix for storing node message history.
    /// Keys will be formatted as: {prefix}:{nodeId}
    /// </summary>
    public string HistoryKeyPrefix { get; set; } = "history";
}

/// <summary>
/// Represents a message in the history with role and content.
/// </summary>
public class HistoryMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Central memory coordinator service.
/// Manages rolling summaries and short-term message history for nodes.
/// </summary>
public interface IMemoryService
{
    /// <summary>
    /// Load the current summary for a node.
    /// Checks Redis first, then falls back to database, then initializes empty.
    /// </summary>
    /// <param name="nodeId">The node identifier</param>
    /// <param name="unitOfWork">XPO UnitOfWork for database access</param>
    /// <returns>The current summary text (may be empty string)</returns>
    Task<string> LoadSummaryAsync(string nodeId, UnitOfWork unitOfWork);

    /// <summary>
    /// Perform rolling summarization after a new message.
    /// Updates both Redis and database with the new summary.
    /// </summary>
    /// <param name="nodeId">The node identifier</param>
    /// <param name="newMessageContent">The content of the new message</param>
    /// <param name="role">The role: "user" or "ai_assistant"</param>
    /// <param name="unitOfWork">XPO UnitOfWork for database access</param>
    /// <returns>The new summary text</returns>
    Task<string> RollingSummarizeAsync(string nodeId, string newMessageContent, string role, UnitOfWork unitOfWork);

    /// <summary>
    /// Add a message to the short-term history in Redis.
    /// Automatically trims to the configured limit.
    /// </summary>
    /// <param name="nodeId">The node identifier</param>
    /// <param name="message">The message to add</param>
    Task AddToHistoryAsync(string nodeId, HistoryMessage message);

    /// <summary>
    /// Get the short-term message history for a node.
    /// </summary>
    /// <param name="nodeId">The node identifier</param>
    /// <returns>List of recent messages</returns>
    Task<List<HistoryMessage>> GetHistoryAsync(string nodeId);

    /// <summary>
    /// Clear all memory for a node (both summary and history).
    /// </summary>
    /// <param name="nodeId">The node identifier</param>
    /// <param name="unitOfWork">XPO UnitOfWork for database access</param>
    Task ClearMemoryAsync(string nodeId, UnitOfWork unitOfWork);
}

public class MemoryService : IMemoryService
{
    private readonly IRedisService _redisService;
    private readonly ISummarizationService _summarizationService;
    private readonly MemoryOptions _options;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(
        IRedisService redisService,
        ISummarizationService summarizationService,
        MemoryOptions options,
        ILogger<MemoryService> logger)
    {
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _summarizationService = summarizationService ?? throw new ArgumentNullException(nameof(summarizationService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the Redis key for a node's summary.
    /// </summary>
    private string GetSummaryKey(string nodeId) => $"{_options.SummaryKeyPrefix}:{nodeId}";

    /// <summary>
    /// Get the Redis key for a node's history.
    /// </summary>
    private string GetHistoryKey(string nodeId) => $"{_options.HistoryKeyPrefix}:{nodeId}";

    /// <summary>
    /// Load the current summary for a node.
    /// </summary>
    public async Task<string> LoadSummaryAsync(string nodeId, UnitOfWork unitOfWork)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            throw new ArgumentNullException(nameof(nodeId));
        }

        var summaryKey = GetSummaryKey(nodeId);

        try
        {
            // Step 1: Check Redis for the summary
            var redisSummary = await _redisService.GetStringAsync(summaryKey);
            
            if (redisSummary != null)
            {
                _logger.LogDebug("Loaded summary for node {NodeId} from Redis ({Length} chars)", 
                    nodeId, redisSummary.Length);
                return redisSummary;
            }

            // Step 2: Redis cache miss - load from database
            _logger.LogDebug("Summary cache miss for node {NodeId}, loading from database", nodeId);
            
            var nodeMemory = unitOfWork.FindObject<NodeMemory>(
                CriteriaOperator.Parse("NodeId = ?", nodeId));

            if (nodeMemory != null && !string.IsNullOrEmpty(nodeMemory.Summary))
            {
                // Found in database - cache in Redis
                await _redisService.SetStringAsync(summaryKey, nodeMemory.Summary);
                
                _logger.LogInformation("Loaded summary for node {NodeId} from database and cached in Redis ({Length} chars)", 
                    nodeId, nodeMemory.Summary.Length);
                return nodeMemory.Summary;
            }

            // Step 3: No summary exists - initialize empty
            var emptySummary = string.Empty;
            
            // Create database record if it doesn't exist
            if (nodeMemory == null)
            {
                nodeMemory = new NodeMemory(unitOfWork)
                {
                    NodeId = nodeId,
                    Summary = emptySummary,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                unitOfWork.Save(nodeMemory);
                await unitOfWork.CommitChangesAsync();
                
                _logger.LogInformation("Created new NodeMemory record for node {NodeId}", nodeId);
            }

            // Cache empty summary in Redis
            await _redisService.SetStringAsync(summaryKey, emptySummary);
            
            _logger.LogDebug("Initialized empty summary for node {NodeId}", nodeId);
            return emptySummary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading summary for node {NodeId}", nodeId);
            throw;
        }
    }

    /// <summary>
    /// Perform rolling summarization after a new message.
    /// </summary>
    public async Task<string> RollingSummarizeAsync(string nodeId, string newMessageContent, string role, UnitOfWork unitOfWork)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            throw new ArgumentNullException(nameof(nodeId));
        }

        if (string.IsNullOrEmpty(newMessageContent))
        {
            _logger.LogWarning("Empty message content for node {NodeId}, skipping summarization", nodeId);
            return await LoadSummaryAsync(nodeId, unitOfWork);
        }

        try
        {
            // Step 1: Load the existing summary
            var oldSummary = await LoadSummaryAsync(nodeId, unitOfWork);

            // Step 2: Call the summarization service
            _logger.LogInformation("Starting rolling summarization for node {NodeId}, role: {Role}, message length: {Length}", 
                nodeId, role, newMessageContent.Length);

            var newSummary = await _summarizationService.SummarizeAsync(
                oldSummary, 
                newMessageContent, 
                role);

            // Step 3: Update Redis
            var summaryKey = GetSummaryKey(nodeId);
            await _redisService.SetStringAsync(summaryKey, newSummary);

            // Step 4: Persist to database
            var nodeMemory = unitOfWork.FindObject<NodeMemory>(
                CriteriaOperator.Parse("NodeId = ?", nodeId));

            if (nodeMemory == null)
            {
                nodeMemory = new NodeMemory(unitOfWork)
                {
                    NodeId = nodeId,
                    Summary = newSummary,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            else
            {
                nodeMemory.Summary = newSummary;
                nodeMemory.UpdatedAt = DateTime.UtcNow;
            }

            unitOfWork.Save(nodeMemory);
            await unitOfWork.CommitChangesAsync();

            _logger.LogInformation("Updated summary for node {NodeId}: {OldLength} -> {NewLength} chars", 
                nodeId, oldSummary.Length, newSummary.Length);

            return newSummary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rolling summarization for node {NodeId}", nodeId);
            throw;
        }
    }

    /// <summary>
    /// Add a message to the short-term history in Redis.
    /// </summary>
    public async Task AddToHistoryAsync(string nodeId, HistoryMessage message)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            throw new ArgumentNullException(nameof(nodeId));
        }

        var historyKey = GetHistoryKey(nodeId);

        try
        {
            // Serialize the message
            var messageJson = JsonSerializer.Serialize(message);

            // Push to the list
            var length = await _redisService.ListRightPushAsync(historyKey, messageJson);

            // Trim if necessary
            if (length > _options.HistoryLimit)
            {
                // Keep only the last HistoryLimit messages
                await _redisService.ListTrimAsync(historyKey, -_options.HistoryLimit, -1);
                
                _logger.LogDebug("Trimmed history for node {NodeId} to {Limit} messages", 
                    nodeId, _options.HistoryLimit);
            }

            _logger.LogDebug("Added message to history for node {NodeId}, role: {Role}", 
                nodeId, message.Role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to history for node {NodeId}", nodeId);
            throw;
        }
    }

    /// <summary>
    /// Get the short-term message history for a node.
    /// </summary>
    public async Task<List<HistoryMessage>> GetHistoryAsync(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            throw new ArgumentNullException(nameof(nodeId));
        }

        var historyKey = GetHistoryKey(nodeId);

        try
        {
            var values = await _redisService.ListRangeAsync(historyKey);
            
            var messages = new List<HistoryMessage>();
            foreach (var value in values)
            {
                try
                {
                    var message = JsonSerializer.Deserialize<HistoryMessage>(value);
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize history message for node {NodeId}", nodeId);
                }
            }

            _logger.LogDebug("Retrieved {Count} messages from history for node {NodeId}", 
                messages.Count, nodeId);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting history for node {NodeId}", nodeId);
            throw;
        }
    }

    /// <summary>
    /// Clear all memory for a node (both summary and history).
    /// </summary>
    public async Task ClearMemoryAsync(string nodeId, UnitOfWork unitOfWork)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            throw new ArgumentNullException(nameof(nodeId));
        }

        try
        {
            // Clear Redis keys
            var summaryKey = GetSummaryKey(nodeId);
            var historyKey = GetHistoryKey(nodeId);

            await _redisService.DeleteKeyAsync(summaryKey);
            await _redisService.DeleteKeyAsync(historyKey);

            // Clear database record
            var nodeMemory = unitOfWork.FindObject<NodeMemory>(
                CriteriaOperator.Parse("NodeId = ?", nodeId));

            if (nodeMemory != null)
            {
                unitOfWork.Delete(nodeMemory);
                await unitOfWork.CommitChangesAsync();
            }

            _logger.LogInformation("Cleared all memory for node {NodeId}", nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing memory for node {NodeId}", nodeId);
            throw;
        }
    }
}
