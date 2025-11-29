using Microsoft.AspNetCore.SignalR;
using NodPT.Data.Models;
using NodPT.API.Hubs;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using NodPT.Data.Interfaces;

namespace NodPT.API.BackgroundServices;

/// <summary>
/// Background service that listens to Redis stream for SignalR updates
/// and forwards them to connected clients via SignalR Hub
/// </summary>
public class SignalRUpdateListener : BackgroundService
{
    private readonly IRedisService _redisService;
    private readonly IHubContext<NodptHub> _hubContext;
    private readonly ILogger<SignalRUpdateListener> _logger;
    private readonly IServiceProvider _serviceProvider;
    private ListenHandle? _listenHandle;

    public SignalRUpdateListener(
        IRedisService redisService,
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
            
            // Extract required fields
            if (!fields.TryGetValue("chatId", out var chatId) || string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("SignalR update missing chatId, skipping");
                return true; // Ack anyway to remove from queue
            }

            if (!fields.TryGetValue("connectionId", out var connectionId) || string.IsNullOrEmpty(connectionId))
            {
                _logger.LogWarning("SignalR update missing connectionId for chatId {ChatId}, skipping", chatId);
                return true; // Ack anyway to remove from queue
            }

            _logger.LogInformation("Processing SignalR update for chatId {ChatId}, connectionId {ConnectionId}", chatId, connectionId);

            // Fetch the AI response from database
            using var scope = _serviceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<UnitOfWork>();

            // Try to parse chatId as int
            if (!int.TryParse(chatId, out var chatIdInt))
            {
                _logger.LogWarning("Invalid chatId format: {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            // Find the original chat message
            var originalMessage = session.FindObject<ChatMessage>(CriteriaOperator.Parse("Oid = ?", chatIdInt));
            if (originalMessage == null)
            {
                _logger.LogWarning("ChatMessage not found for chatId {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            // Get the AI response (latest assistant message for the same node)
            using var aiResponses = new XPCollection<ChatMessage>(session,
                CriteriaOperator.Parse("Node.Id = ? AND Sender = ? AND Timestamp >= ?", 
                    originalMessage.Node?.Id, "assistant", originalMessage.Timestamp),
                new SortProperty("Timestamp", DevExpress.Xpo.DB.SortingDirection.Descending));

            if (aiResponses.Count == 0)
            {
                _logger.LogWarning("No AI response found for chatId {ChatId}", chatId);
                return false; // Don't ack, might be too early, retry later
            }

            var latestResponse = aiResponses[0];

            // Send to the specific client connection via SignalR
            await _hubContext.Clients.Client(connectionId).SendAsync(
                "ReceiveAIResponse",
                new
                {
                    chatId = chatId,
                    messageId = latestResponse.Oid,
                    content = latestResponse.Message,
                    sender = latestResponse.Sender,
                    timestamp = latestResponse.Timestamp,
                    nodeId = originalMessage.Node?.Id
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
