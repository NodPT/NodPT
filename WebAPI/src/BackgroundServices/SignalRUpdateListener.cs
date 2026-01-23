using Microsoft.AspNetCore.SignalR;
using NodPT.Data.Models;
using NodPT.API.Hubs;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using RedisService.Queue;

namespace NodPT.API.BackgroundServices;

/// <summary>
/// Background service that listens to Redis stream for SignalR updates
/// and forwards them to connected clients via SignalR Hub
/// </summary>
public class SignalRUpdateListener : BackgroundService
{
    private readonly RedisQueueService _redisService;
    private readonly IHubContext<NodptHub> _hubContext;
    private readonly ILogger<SignalRUpdateListener> _logger;
    private readonly IServiceProvider _serviceProvider;
    private ListenHandle? _listenHandle;

    public SignalRUpdateListener(
        RedisQueueService redisService,
        IHubContext<NodptHub> hubContext,
        ILogger<SignalRUpdateListener> logger,
        IServiceProvider serviceProvider)
    {
        _redisService = redisService;
        _hubContext = hubContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SignalRUpdateListener starting...");

        var options = new ListenOptions
        {
            BatchSize = 10,
            Concurrency = 5,
            ClaimIdleThresholdMs = 60000,
            MaxRetries = 3,
            PollDelayMs = 1000,
            CreateStreamIfMissing = true,
            ClaimPendingOnStartup = true
        };

        // Generate unique consumer name for this instance
        var consumerName = $"webapi-{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";

        _listenHandle = _redisService.Listen(
            streamKey: "signalr:updates",
            group: "signalr",
            consumerName: consumerName,
            handler: HandleSignalRUpdate,
            options: options);

        _logger.LogInformation("SignalRUpdateListener is now listening to signalr:updates stream");

        // Wait for cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task<bool> HandleSignalRUpdate(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        try
        {
            var fields = envelope.Fields;
            
            // Check if this is a thinking/progress message (doesn't have chatId, has messageType)
            if (fields.TryGetValue("messageType", out var messageType) && messageType == "thinking")
            {
                // Handle thinking message - no database lookup needed
                return await HandleThinkingMessage(fields, cancellationToken);
            }
            
            // Extract required fields for regular AI response messages
            if (!fields.TryGetValue("chatId", out var chatId) || string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("SignalR update missing chatId, skipping");
                return true; // Ack anyway to remove from queue
            }

            // Try to get connectionId from Redis payload first
            string? connectionId = null;
            if (fields.TryGetValue("connectionId", out var redisConnectionId) && !string.IsNullOrEmpty(redisConnectionId))
            {
                connectionId = redisConnectionId;
                _logger.LogDebug("Processing SignalR update for chatId {ChatId}, connectionId {ConnectionId} (from Redis)", chatId, connectionId);
            }
            else
            {
                // Fallback: Fetch connectionId from database for backward compatibility
                _logger.LogWarning("SignalR update missing connectionId in Redis for chatId {ChatId}, attempting database fallback", chatId);
            }

            var isSolutionOutput = fields.TryGetValue("solutionOutput", out var solutionFlag) && solutionFlag == "true";

            // Fetch the AI response from database
            using var scope = _serviceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<UnitOfWork>();

            // Try to parse chatId as int
            if (!int.TryParse(chatId, out var chatIdInt))
            {
                _logger.LogWarning("Invalid chatId format: {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            // Fetch the AI response message
            // (the chatId in Redis is the database ID of the AI response message)
            var aiResponseMessage = session.FindObject<ChatMessage>(CriteriaOperator.Parse("Oid = ?", chatIdInt));
            if (aiResponseMessage == null)
            {
                _logger.LogWarning("ChatMessage not found for chatId {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            // If connectionId not in Redis payload, get it from the database message
            if (connectionId == null)
            {
                connectionId = aiResponseMessage.ConnectionId;

                if (string.IsNullOrEmpty(connectionId))
                {
                    _logger.LogWarning("ConnectionId not found for chatId {ChatId}, skipping SignalR delivery", chatId);
                    return true; // Ack anyway to remove from queue
                }

                _logger.LogDebug("Retrieved connectionId {ConnectionId} from database for chatId {ChatId}", connectionId, chatId);
            }

            // Send to the specific client connection via SignalR using the standard ReceiveMessage event
            // This consolidates all message types under one event to reduce redundancy
            await _hubContext.Clients.Client(connectionId).SendAsync(
                "ReceiveMessage",
                new
                {
                    chatId = chatId,
                    messageId = aiResponseMessage.Oid,
                    content = aiResponseMessage.Message,
                    sender = aiResponseMessage.Sender,
                    timestamp = aiResponseMessage.Timestamp,
                    nodeId = aiResponseMessage.Node?.Id,
                    messageType = "ai", // Indicates this is an AI response message
                    solutionOutput = isSolutionOutput
                },
                cancellationToken);

            _logger.LogInformation("Sent AI response to client {ConnectionId} for chatId {ChatId}", connectionId, chatId);

            return true; // Success, ack the message
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SignalR update for entry {EntryId}", envelope.EntryId);
            return false; // Fail, will retry
        }
    }

    /// <summary>
    /// Handle thinking/progress messages sent from Executor
    /// These messages show real-time progress to the user
    /// </summary>
    private async Task<bool> HandleThinkingMessage(Dictionary<string, string> fields, CancellationToken cancellationToken)
    {
        try
        {
            // Extract required fields for thinking messages
            if (!fields.TryGetValue("connectionId", out var connectionId) || string.IsNullOrEmpty(connectionId))
            {
                _logger.LogWarning("Thinking message missing connectionId, skipping");
                return true; // Ack anyway
            }

            if (!fields.TryGetValue("content", out var content) || string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Thinking message missing content, skipping");
                return true; // Ack anyway
            }

            fields.TryGetValue("nodeId", out var nodeId);
            fields.TryGetValue("timestamp", out var timestamp);

            // Send thinking message to the specific client via SignalR using the standard ReceiveMessage event
            // This consolidates all message types under one event to reduce redundancy
            await _hubContext.Clients.Client(connectionId).SendAsync(
                "ReceiveMessage",
                new
                {
                    chatId = (string?)null, // No chatId for thinking messages
                    messageId = (int?)null, // No messageId for thinking messages
                    content = content,
                    sender = "assistant",
                    timestamp = timestamp ?? DateTime.UtcNow.ToString("o"),
                    nodeId = nodeId,
                    messageType = "thinking", // Indicates this is a progress/thinking message
                    thinking = true // Additional flag for backward compatibility
                },
                cancellationToken);

            _logger.LogDebug("Sent thinking message to client {ConnectionId}: {Content}", connectionId, content);

            return true; // Success, ack the message
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling thinking message");
            return false; // Fail, will retry
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SignalRUpdateListener stopping...");

        if (_listenHandle != null)
        {
            await _redisService.StopListen(_listenHandle);
        }

        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("SignalRUpdateListener stopped");
    }
}
