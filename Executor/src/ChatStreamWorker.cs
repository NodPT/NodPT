using BackendExecutor.Services;
using NodPT.Data.Services;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;

namespace BackendExecutor;

/// <summary>
/// Background service that listens to Redis stream for chat jobs,
/// processes them with LLM, and publishes results.
/// 
/// Workflow:
/// 1. Pull new WebAPI data from Redis (data A)
/// 2. Mark as processing to avoid duplicated processing
/// 3. Get chatId and message content from the data
/// 4. Use chatId to query chat data from database
/// 5. Get nodeId from chat data to get node data
/// 6. Get projectId from node data to get project data
/// 7. Get templateId from project data to get template data
/// 8. Get prompts from template data based on Node's level
/// 9. Get model name from template data based on Node's level
/// 10. Prepare Ollama data object with proper JSON format
/// 11. Send message to Ollama
/// 12. Wait for response data
/// 13. Extract content from response data
/// 14. Create new message data with response content
/// 15. Save new message data to database to get new chatId
/// 16. Prepare new Redis data (data B) with new chatId
/// 17. Acknowledge the Redis data from WebAPI (data A)
/// </summary>
public class ChatStreamWorker : BackgroundService
{
    private readonly ILogger<ChatStreamWorker> _logger;
    private readonly IRedisService _redisService;
    private readonly ILlmChatService _llmChatService;
    private readonly IServiceProvider _serviceProvider;
    private ListenHandle? _listenHandle;

    public ChatStreamWorker(
        ILogger<ChatStreamWorker> logger,
        IRedisService redisService,
        ILlmChatService llmChatService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _redisService = redisService;
        _llmChatService = llmChatService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChatStreamWorker starting...");

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

        _logger.LogInformation("ChatStreamWorker is now listening to jobs:chat stream");

        // Wait for cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task<bool> HandleChatJob(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        try
        {
            var fields = envelope.Fields;
            
            // Step 1-2: Extract required fields from Redis data (data A)
            // The message is marked as processing by Redis consumer group automatically
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

            _logger.LogInformation("Processing chat job: ChatId={ChatId}, ConnectionId={ConnectionId}", chatId, connectionId);

            // Step 3: Parse chatId
            if (!int.TryParse(chatId, out var chatIdInt))
            {
                _logger.LogWarning("Invalid chatId format: {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            // Create database session
            using var scope = _serviceProvider.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<UnitOfWork>();

            // Step 4: Use chatId to query chat data from database
            var chatMessage = session.FindObject<ChatMessage>(
                CriteriaOperator.Parse("Oid = ?", chatIdInt));

            if (chatMessage == null)
            {
                _logger.LogWarning("ChatMessage not found for chatId {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            var userMessage = chatMessage.Message ?? "";
            _logger.LogInformation("Retrieved chat message for chatId {ChatId}: {MessageLength} chars", 
                chatId, userMessage.Length);

            // Step 5: Get nodeId from chat data to get node data
            var node = chatMessage.Node;
            if (node == null)
            {
                _logger.LogWarning("Node not found for chatId {ChatId}", chatId);
                return true; // Ack anyway - no node association
            }

            _logger.LogInformation("Node found: NodeId={NodeId}, Level={Level}, MessageType={MessageType}", 
                node.Id, node.Level, node.MessageType);

            // Step 6: Get projectId from node data to get project data
            var project = node.Project;
            if (project == null)
            {
                _logger.LogWarning("Project not found for nodeId {NodeId}", node.Id);
                return true; // Ack anyway - no project association
            }

            var firebaseUid = project.User?.FirebaseUid ?? "";
            _logger.LogInformation("Project found: ProjectId={ProjectId}, UserId={FirebaseUid}", 
                project.Oid, firebaseUid);

            // Step 7: Get templateId from project data to get template data
            var template = project.Template;
            if (template == null)
            {
                _logger.LogWarning("Template not found for projectId {ProjectId}", project.Oid);
                return true; // Ack anyway - no template association
            }

            _logger.LogInformation("Template found: TemplateId={TemplateId}, Name={TemplateName}", 
                template.Oid, template.Name);

            // Step 8: Get prompts from template data based on Node's level and message type
            var matchingPrompts = node.MatchingPrompts;
            var promptContents = matchingPrompts
                .Where(p => !string.IsNullOrEmpty(p.Content))
                .Select(p => p.Content!)
                .ToList();

            _logger.LogInformation("Found {PromptCount} matching prompts for Level={Level}, MessageType={MessageType}", 
                promptContents.Count, node.Level, node.MessageType);

            // Step 9: Get model name from template data based on Node's level
            var matchingAiModel = node.MatchingAIModel;
            var modelName = matchingAiModel?.ModelIdentifier ?? "llama3.2:3b";

            _logger.LogInformation("Using model: {ModelName} (from AIModel: {AIModelName})", 
                modelName, matchingAiModel?.Name ?? "default");

            // Step 10: Prepare Ollama data object with messages array
            var messages = new List<OllamaMessage>();
            
            // Add system prompts first
            foreach (var promptContent in promptContents)
            {
                messages.Add(new OllamaMessage { Role = "system", Content = promptContent });
            }
            
            // Add user message
            messages.Add(new OllamaMessage { Role = "user", Content = userMessage });
            
            // Get temperature from AIModel if available, otherwise default to 0
            var temperature = matchingAiModel?.Temperature ?? 0;

            var ollamaRequest = new OllamaRequest
            {
                Model = modelName,
                Messages = messages,
                Options = new OllamaOptions { Temperature = temperature },
                Stream = false
            };

            _logger.LogInformation("Prepared Ollama request with {MessageCount} messages for chatId {ChatId}", 
                messages.Count, chatId);

            // Step 11-12: Send message to Ollama and wait for response
            var aiResponse = await _llmChatService.SendChatRequestAsync(ollamaRequest, cancellationToken);

            _logger.LogInformation("Received AI response with {Length} chars for chatId {ChatId}", 
                aiResponse.Length, chatId);

            // Step 13-14: Extract content and create new message data
            var aiMessage = new ChatMessage(session)
            {
                Sender = "assistant",
                Message = aiResponse,
                Node = node,
                User = chatMessage.User,
                Timestamp = DateTime.UtcNow,
                ConnectionId = connectionId
            };

            // Step 15: Save new message data to database to get new chatId
            session.Save(aiMessage);
            await session.CommitChangesAsync(cancellationToken);

            _logger.LogInformation("Saved AI response: NewChatId={NewChatId} for original chatId {ChatId}", 
                aiMessage.Oid, chatId);

            // Step 16: Prepare new Redis data (data B) with new chatId only
            // Other data (connectionId, nodeId, userId, projectId) are already saved in the ChatMessage
            var resultEnvelope = new Dictionary<string, string>
            {
                { "chatId", aiMessage.Oid.ToString() }
            };

            var entryId = await _redisService.Add("signalr:updates", resultEnvelope);

            _logger.LogInformation("Published result to signalr:updates: EntryId={EntryId}, NewChatId={NewChatId}", 
                entryId, aiMessage.Oid);

            // Step 17: Acknowledge the Redis data (data A) - handled by returning true
            return true; // Success, ack the message
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat job for entry {EntryId}", envelope.EntryId);
            return false; // Fail, will retry
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ChatStreamWorker stopping...");

        if (_listenHandle != null)
        {
            await _redisService.StopListen(_listenHandle);
        }

        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("ChatStreamWorker stopped");
    }
}
