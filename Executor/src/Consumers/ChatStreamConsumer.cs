using BackendExecutor.Services;
using NodPT.Data.Services;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;

namespace BackendExecutor.Consumers;

public interface IChatStreamConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}

/// <summary>
/// Chat job consumer using Redis Streams (replaces list-based consumer)
/// </summary>
public class ChatStreamConsumer : IChatStreamConsumer
{
    private readonly ILogger<ChatStreamConsumer> _logger;
    private readonly IRedisService _redisService;
    private readonly ILlmChatService _llmChatService;
    private readonly IServiceProvider _serviceProvider;
    private ListenHandle? _listenHandle;

    public ChatStreamConsumer(
        ILogger<ChatStreamConsumer> logger,
        IRedisService redisService,
        ILlmChatService llmChatService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _redisService = redisService;
        _llmChatService = llmChatService;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ChatStreamConsumer: Starting to consume from jobs:chat stream");

        var options = new ListenOptions
        {
            BatchSize = 10,
            Concurrency = 3,
            ClaimIdleThresholdMs = 60000,
            MaxRetries = 3,
            PollDelayMs = 1000,
            CreateStreamIfMissing = true,
            ClaimPendingOnStartup = true
        };

        // Generate unique consumer name for this instance
        var consumerName = $"executor-{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";

        _listenHandle = _redisService.Listen(
            streamKey: "jobs:chat",
            group: "executor",
            consumerName: consumerName,
            handler: HandleChatJob,
            options: options);

        _logger.LogInformation("ChatStreamConsumer: Now listening to jobs:chat stream");

        return Task.CompletedTask;
    }

    private async Task<bool> HandleChatJob(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        try
        {
            var fields = envelope.Fields;
            
            // Extract required fields
            if (!fields.TryGetValue("chatId", out var chatId) || string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("Chat job missing chatId, skipping");
                return true; // Ack anyway to remove from queue
            }

            if (!fields.TryGetValue("connectionId", out var connectionId) || string.IsNullOrEmpty(connectionId))
            {
                _logger.LogWarning("Chat job missing connectionId for chatId {ChatId}, skipping", chatId);
                return true; // Ack anyway to remove from queue
            }

            fields.TryGetValue("nodeId", out var nodeId);
            fields.TryGetValue("userId", out var userId);
            fields.TryGetValue("projectId", out var projectId);
            fields.TryGetValue("model", out var modelName);

            _logger.LogInformation("Processing chat job: ChatId={ChatId}, ConnectionId={ConnectionId}, Model={Model}", 
                chatId, connectionId, modelName);

            // Load chat message from database
            using var scope = _serviceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<UnitOfWork>();

            if (!int.TryParse(chatId, out var chatIdInt))
            {
                _logger.LogWarning("Invalid chatId format: {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            var chatMessage = session.FindObject<NodPT.Data.Models.ChatMessage>(
                CriteriaOperator.Parse("Oid = ?", chatIdInt));

            if (chatMessage == null)
            {
                _logger.LogWarning("ChatMessage not found for chatId {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            var userMessage = chatMessage.Message ?? "";

            // Determine model to use
            var model = !string.IsNullOrEmpty(modelName) ? modelName : ExtractModelName(nodeId);

            _logger.LogInformation("Using model: {Model} for chatId {ChatId}", model, chatId);

            // Send request to LLM endpoint
            var aiResponse = await SendToLlm(userMessage, model, cancellationToken);

            // Save AI response to database
            var aiMessage = new NodPT.Data.Models.ChatMessage(session)
            {
                Sender = "assistant",
                Message = aiResponse,
                Node = chatMessage.Node,
                User = chatMessage.User,
                Timestamp = DateTime.UtcNow
            };
            session.Save(aiMessage);
            await session.CommitChangesAsync(cancellationToken);

            _logger.LogInformation("Saved AI response for chatId {ChatId}, responseId {ResponseId}", chatId, aiMessage.Oid);

            // Publish result to signalr:updates stream
            var resultEnvelope = new Dictionary<string, string>
            {
                { "chatId", chatId },
                { "connectionId", connectionId },
                { "responseId", aiMessage.Oid.ToString() },
                { "nodeId", nodeId ?? "" },
                { "userId", userId ?? "" },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };

            var entryId = await _redisService.Add("signalr:updates", resultEnvelope);

            _logger.LogInformation("Published result to signalr:updates for chatId {ChatId}, entryId {EntryId}", chatId, entryId);

            return true; // Success, ack the message
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat job for entry {EntryId}", envelope.EntryId);
            return false; // Fail, will retry
        }
    }

    private string ExtractModelName(string? nodeId)
    {
        // Extract model name from node context if available
        // For now, use a default model
        if (string.IsNullOrEmpty(nodeId))
        {
            return "llama3.2:3b";
        }

        // Map node levels to model names (placeholder logic)
        return "llama3.2:3b";
    }

    private async Task<string> SendToLlm(string message, string modelName, CancellationToken cancellationToken)
    {
        try
        {
            // Use the LlmChatService to send the message
            var response = await _llmChatService.SendChatMessageAsync(
                message,
                modelName,
                maxTokens: 512,
                cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LLM endpoint for model {ModelName}", modelName);
            // Return an error message instead of throwing to keep the system running
            return $"Error: Unable to process message with model {modelName}. {ex.Message}";
        }
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("ChatStreamConsumer: Stopping...");

        if (_listenHandle != null)
        {
            await _redisService.StopListen(_listenHandle);
        }

        _logger.LogInformation("ChatStreamConsumer: Stopped");
    }
}
