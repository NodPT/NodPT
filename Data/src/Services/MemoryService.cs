using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NodPT.Data.Models;
using System.Text.Json;
using NodPT.Data.DTOs;
using NodPT.Data.Interfaces;
using RedisService.Cache;

namespace NodPT.Data.Services;

public class MemoryService : IMemoryService
{
    private readonly RedisCacheService _redisService;
    private readonly ISummarizationService _summarizationService;
    private readonly MemoryOptions _options;
    private readonly ILogger<MemoryService> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// Counter for tracking consecutive summarization failures for monitoring purposes.
    /// </summary>
    private int _failedSummarizationCount = 0;

    public MemoryService(
        RedisCacheService redisService,
        ISummarizationService summarizationService,
        MemoryOptions options,
        ILogger<MemoryService> logger,
        IServiceProvider serviceProvider)
    {
        _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
        _summarizationService = summarizationService ?? throw new ArgumentNullException(nameof(summarizationService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
            var redisSummary = await _redisService.Get(summaryKey);
            
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
                await _redisService.Set(summaryKey, nodeMemory.Summary);
                
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
                
                _logger.LogInformation("Created new NodeMemory record for node {NodeId}", nodeId);
            }

            // Cache empty summary in Redis
            await _redisService.Set(summaryKey, emptySummary);
            
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
            await _redisService.Set(summaryKey, newSummary);

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
    /// Queue rolling summarization to run in the background (non-blocking).
    /// This allows the chat flow to continue without waiting for summarization.
    /// </summary>
    public void QueueSummarization(string nodeId, string newMessageContent, string role)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            _logger.LogWarning("Cannot queue summarization: nodeId is null or empty");
            return;
        }

        if (string.IsNullOrEmpty(newMessageContent))
        {
            _logger.LogDebug("Skipping summarization for node {NodeId}: empty message content", nodeId);
            return;
        }

        _logger.LogInformation("Queuing background summarization for node {NodeId}, role: {Role}", nodeId, role);

        // Fire and forget - run summarization in a background task
        _ = Task.Run(async () =>
        {
            try
            {
                // Create a new scope for the database context
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();

                await RollingSummarizeAsync(nodeId, newMessageContent, role, unitOfWork);
                
                // Reset failure count on success
                Interlocked.Exchange(ref _failedSummarizationCount, 0);
                
                _logger.LogDebug("Background summarization completed for node {NodeId}", nodeId);
            }
            catch (Exception ex)
            {
                var failCount = Interlocked.Increment(ref _failedSummarizationCount);
                _logger.LogError(ex, "Background summarization failed for node {NodeId} (failure count: {FailCount})", 
                    nodeId, failCount);
                // Don't rethrow - this is a fire-and-forget operation
            }
        });
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
            var length = await _redisService.Update(historyKey, messageJson);

            // Trim if necessary
            if (length > _options.HistoryLimit)
            {
                // Keep only the last HistoryLimit messages
                await _redisService.TrimList(historyKey, -_options.HistoryLimit, -1);
                
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
            var values = await _redisService.Range(historyKey);
            
            var messages = new List<HistoryMessage>();
            var errorCount = 0;
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
                    errorCount++;
                    if (errorCount > values.Count / 2) // More than 50% corrupted
                    {
                        _logger.LogError("History for node {NodeId} is severely corrupted ({ErrorCount}/{Total})", 
                            nodeId, errorCount, values.Count);
                        throw new InvalidOperationException($"History data corrupted for node {nodeId}");
                    }
                    _logger.LogWarning(ex, "Failed to deserialize history message {Index} for node {NodeId}", 
                        messages.Count, nodeId);
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

            await _redisService.Remove(summaryKey);
            await _redisService.Remove(historyKey);

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
