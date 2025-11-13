using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;
using NodPT.SignalR.Hubs;
using NodPT.SignalR.Models;

namespace NodPT.SignalR.Services;

public class RedisStreamListener : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<NodptHub> _hubContext;
    private readonly ILogger<RedisStreamListener> _logger;
    private readonly IConfiguration _configuration;
    private const string StreamKey = "signalr:updates";
    private const string ConsumerGroup = "signalr-hub-group";
    private const string ConsumerName = "signalr-hub-consumer";

    // Configuration values with defaults
    private readonly int _messageBatchSize;
    private readonly int _pollingDelayMs;
    private readonly int _errorRetryDelayMs;

    public RedisStreamListener(
        IConnectionMultiplexer redis,
        IHubContext<NodptHub> hubContext,
        ILogger<RedisStreamListener> logger,
        IConfiguration configuration)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
        _configuration = configuration;

        // Load configuration with defaults
        _messageBatchSize = _configuration.GetValue<int>("Redis:StreamListener:MessageBatchSize", 10);
        _pollingDelayMs = _configuration.GetValue<int>("Redis:StreamListener:PollingDelayMs", 100);
        _errorRetryDelayMs = _configuration.GetValue<int>("Redis:StreamListener:ErrorRetryDelayMs", 1000);

        _logger.LogInformation($"RedisStreamListener configured: MessageBatchSize={_messageBatchSize}, PollingDelayMs={_pollingDelayMs}, ErrorRetryDelayMs={_errorRetryDelayMs}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RedisStreamListener starting...");

        var db = _redis.GetDatabase();

        // Try to create consumer group (ignore error if already exists)
        try
        {
            await db.StreamCreateConsumerGroupAsync(StreamKey, ConsumerGroup, "0-0");
            _logger.LogInformation($"Created consumer group '{ConsumerGroup}' on stream '{StreamKey}'");
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            _logger.LogInformation($"Consumer group '{ConsumerGroup}' already exists");
        }

        _logger.LogInformation("RedisStreamListener is now listening to Redis stream...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Read messages from the stream
                var entries = await db.StreamReadGroupAsync(
                    StreamKey,
                    ConsumerGroup,
                    ConsumerName,
                    ">",
                    count: _messageBatchSize,
                    noAck: false);

                if (entries.Length > 0)
                {
                    foreach (var entry in entries)
                    {
                        try
                        {
                            await ProcessMessageAsync(entry, db, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing message {entry.Id}. Acknowledging to prevent redelivery.");
                            // Acknowledge the message to prevent infinite redelivery loop
                            try
                            {
                                await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
                            }
                            catch (Exception ackEx)
                            {
                                _logger.LogError(ackEx, $"Failed to acknowledge message {entry.Id} after processing error");
                            }
                        }
                    }
                }
                else
                {
                    // No messages, wait a bit before polling again
                    await Task.Delay(_pollingDelayMs, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error reading from Redis stream");
                await Task.Delay(_errorRetryDelayMs, stoppingToken);
            }
        }

        _logger.LogInformation("RedisStreamListener stopped");
    }

    private async Task ProcessMessageAsync(StreamEntry entry, IDatabase db, CancellationToken cancellationToken)
    {
        // Parse the Redis stream entry into NodeMessage
        var message = ParseStreamEntry(entry);

        if (message == null)
        {
            _logger.LogWarning($"Failed to parse message {entry.Id}");
            await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
            return;
        }

        _logger.LogInformation($"Received message: Type={message.Type}, NodeId={message.NodeId}, ProjectId={message.ProjectId}, UserId={message.UserId}");

        // Route the message to the appropriate client(s)
        await RouteMessageAsync(message, cancellationToken);

        // Acknowledge the message after successful routing
        await db.StreamAcknowledgeAsync(StreamKey, ConsumerGroup, entry.Id);
    }

    private NodeMessage? ParseStreamEntry(StreamEntry entry)
    {
        try
        {
            var message = new NodeMessage();

            foreach (var field in entry.Values)
            {
                var fieldName = field.Name.ToString();
                var fieldValue = field.Value.ToString();

                switch (fieldName.ToLowerInvariant())
                {
                    case "messageid":
                        message.MessageId = fieldValue;
                        break;
                    case "nodeid":
                        message.NodeId = fieldValue;
                        break;
                    case "projectid":
                        message.ProjectId = fieldValue;
                        break;
                    case "userid":
                        message.UserId = fieldValue;
                        break;
                    case "clientconnectionid":
                        message.ClientConnectionId = fieldValue;
                        break;
                    case "workflowgroup":
                        message.WorkflowGroup = fieldValue;
                        break;
                    case "type":
                        message.Type = fieldValue;
                        break;
                    case "payload":
                        message.Payload = fieldValue;
                        break;
                    case "timestamp":
                        if (DateTime.TryParse(fieldValue, out var timestamp))
                        {
                            message.Timestamp = timestamp;
                        }
                        break;
                }
            }

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing stream entry");
            return null;
        }
    }

    private async Task RouteMessageAsync(NodeMessage message, CancellationToken cancellationToken)
    {
        // Prepare the message data to send to clients
        var messageData = new
        {
            messageId = message.MessageId,
            nodeId = message.NodeId,
            projectId = message.ProjectId,
            userId = message.UserId,
            type = message.Type,
            payload = message.Payload,
            timestamp = message.Timestamp,
            workflowGroup = message.WorkflowGroup
        };

        var delivered = false;

        // Route based on ClientConnectionId first (most specific)
        if (!string.IsNullOrEmpty(message.ClientConnectionId))
        {
            try
            {
                _logger.LogInformation($"Routing message to specific client: {message.ClientConnectionId}");
                await _hubContext.Clients.Client(message.ClientConnectionId).SendAsync("ReceiveNodeUpdate", messageData, cancellationToken);
                delivered = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to deliver message to client {message.ClientConnectionId}, attempting fallback routing");
            }
        }

        // If specific client routing failed or wasn't attempted, try fallback routing
        if (!delivered)
        {
            // Route to workflow group if specified
            if (!string.IsNullOrEmpty(message.WorkflowGroup))
            {
                _logger.LogInformation($"Routing message to workflow group: {message.WorkflowGroup}");
                await _hubContext.Clients.Group(message.WorkflowGroup).SendAsync("ReceiveNodeUpdate", messageData, cancellationToken);
                delivered = true;
            }
            // Route to user-specific group if specified
            else if (!string.IsNullOrEmpty(message.UserId))
            {
                var userGroup = $"user:{message.UserId}";
                _logger.LogInformation($"Routing message to user group: {userGroup}");
                await _hubContext.Clients.Group(userGroup).SendAsync("ReceiveNodeUpdate", messageData, cancellationToken);
                delivered = true;
            }
        }

        if (!delivered)
        {
            _logger.LogWarning($"Message {message.MessageId} could not be delivered - no valid routing target");
        }
    }
}
