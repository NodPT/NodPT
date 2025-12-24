using BackendExecutor.Services;
using NodPT.Data.Services;
using NodPT.Data.Models;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using NodPT.Data.DTOs;
using RedisService.Queue;
using RedisService.Cache;

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
/// 10. Load memory summary and history for the node
/// 11. Prepare Ollama data object with summary, history, prompts, and user message
/// 12. Send message to Ollama
/// 13. Wait for response data
/// 14. Extract content from response data
/// 15. Create new message data with response content
/// 16. Save new message data to database to get new chatId
/// 17. Update memory: add user message to history, trigger rolling summarization
/// 18. Update memory: add AI message to history, trigger rolling summarization
/// 19. Prepare new Redis data (data B) with new chatId
/// 20. Acknowledge the Redis data from WebAPI (data A)
/// </summary>
public class ChatStreamWorker : BackgroundService
{
    private readonly ILogger<ChatStreamWorker> _logger;
    private readonly RedisQueueService _redisService;
    private readonly LlmChatService _llmChatService;
    private readonly MemoryService _memoryService;
    private ListenHandle? _listenHandle;

    public ChatStreamWorker(
        ILogger<ChatStreamWorker> logger,
        RedisQueueService redisService,
        LlmChatService llmChatService,
        MemoryService memoryService)
    {
        _logger = logger;
        _redisService = redisService;
        _llmChatService = llmChatService;
        _memoryService = memoryService;
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
            
            // Log high-level information about the Redis job entry
            _logger.LogInformation("=== Processing Redis Job Entry ===");
            _logger.LogInformation("Entry ID: {EntryId}", envelope.EntryId);
            _logger.LogInformation("Stream Key: {StreamKey}", envelope.StreamKey);
            _logger.LogInformation("Payload Fields Count: {FieldCount}", fields.Count);
            
            // Log detailed payload fields only at Debug level to avoid exposing sensitive data
            _logger.LogDebug("Redis job payload fields: {@Fields}", fields);
            
            // Step 1-2: Extract required fields from Redis data (data A)
            // The message is marked as processing by Redis consumer group automatically
            if (!fields.TryGetValue("chatId", out var chatId) || string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("Chat job missing chatId, skipping. Payload: {@Fields}", fields);
                return true; // Ack anyway to remove from queue
            }

            _logger.LogInformation("Processing chat job: ChatId={ChatId}", chatId);

            // Step 3: Parse chatId
            if (!int.TryParse(chatId, out var chatIdInt))
            {
                _logger.LogWarning("Invalid chatId format: {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            // Create database session
            var session = DatabaseHelper.GetSession();
            if (session == null)
            {
                _logger.LogError("Failed to create database session");
                return false; // Fail, will retry
            }

            // Step 4: Use chatId to query chat data from database
            ChatMessage chatMessage = session.GetObjectByKey<ChatMessage>(chatIdInt);

            if (chatMessage == null)
            {
                _logger.LogWarning("ChatMessage not found for chatId {ChatId}", chatId);
                return true; // Ack anyway to remove from queue
            }

            // extract chat message content
            var userMessage = chatMessage.Message ?? "";
            _logger.LogInformation("Retrieved chat message for chatId {ChatId}: {MessageLength} chars", 
                chatId, userMessage.Length);

            if (string.IsNullOrEmpty(userMessage))
            {
                _logger.LogWarning("Chat message is empty for chatId {ChatId}", chatId);
                return true; // Ack anyway - nothing to process
            }

            // Step 5: Get nodeId from chat data to get node data
            Node? node = chatMessage.Node;
            if (node == null)
            {
                _logger.LogWarning("Node not found for chatId {ChatId}", chatId);
                return true; // Ack anyway - no node association
            }

            // Step 6: Get projectId from node data to get project data
            var project = node.Project;
            if (project == null)
            {
                _logger.LogWarning("Project not found for chatId {chatId}", chatId);
                return true; // Ack anyway - no project association
            }

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
            var matchingPrompts = node.GetMatchingPrompts();
            var promptContents = matchingPrompts
                .Where(p => !string.IsNullOrEmpty(p.Content))
                .Select(p => p.Content!)
                .ToList();

            _logger.LogInformation("Found {PromptCount} matching prompts for Level={Level}, MessageType={MessageType}", 
                promptContents.Count, node.Level, node.MessageType);

            // Step 9: Get model name from template data based on Node's level
            var matchingAiModel = node.GetMatchingAIModel();
            var modelName = matchingAiModel?.ModelIdentifier ?? "llama3.2:3b";

            _logger.LogInformation("Using model: {ModelName} (from AIModel: {AIModelName})", 
                modelName, matchingAiModel?.Name ?? "default");

            string nodeId = node.Id!;

            // Step 10: Load memory summary and history for the node
            var summary = await _memoryService.LoadSummaryAsync(nodeId, session);
            var history = await _memoryService.GetHistoryAsync(nodeId);

            _logger.LogInformation("Loaded memory for node {NodeId}: Summary={SummaryLength} chars, History={HistoryCount} messages",
                nodeId, summary.Length, history.Count);

            // Step 11: Prepare Ollama data object with messages array
            var messages = new List<OllamaMessage>();
            
            // Add system prompts first
            foreach (var promptContent in promptContents)
            {
                messages.Add(new OllamaMessage { role = "system", content = promptContent });
            }

            // Add memory summary as system context if available
            if (!string.IsNullOrEmpty(summary))
            {
                var summaryContext = $"[Conversation Memory]\n{summary}";
                messages.Add(new OllamaMessage { role = "system", content = summaryContext });
            }

            // Add recent history messages
            foreach (var historyMessage in history)
            {
                messages.Add(new OllamaMessage 
                { 
                    role = historyMessage.Role, 
                    content = historyMessage.Content 
                });
            }
            
            // Add current user message
            messages.Add(new OllamaMessage { role = "user", content = userMessage });
            
            // Build Ollama request with options from AIModel
            var ollamaRequest = new OllamaRequest
            {
                model = modelName,
                messages = messages,
                options = LlmChatService.BuildOptionsFromAIModel(matchingAiModel)
            };

            _logger.LogInformation("Prepared Ollama request with {MessageCount} messages for chatId {ChatId} (including memory context)", 
                messages.Count, chatId);
            _logger.LogInformation("Ollama Request Details - Model: {Model}, SystemPrompts: {SystemPromptCount}, History: {HistoryCount}, UserMessage Length: {UserMessageLength}", 
                modelName, promptContents.Count, history.Count, userMessage.Length);
            
            // Log the endpoint being used
            var endpoint = !string.IsNullOrEmpty(matchingAiModel?.EndpointAddress) 
                ? matchingAiModel.EndpointAddress 
                : "default endpoint from config";
            _logger.LogInformation("Using LLM Endpoint: {Endpoint}", endpoint);

            //! STEP 12-13: SEND MESSAGE TO OLLAMA AND WAIT FOR RESPONSE
            // Use AIModel's endpoint and options if available
            _logger.LogInformation("=== Sending Request to LLM ===");
            _logger.LogInformation("ChatId: {ChatId}, NodeId: {NodeId}, Model: {Model}", chatId, nodeId, modelName);
            
            var aiResponse = await _llmChatService.SendChatRequestAsync(ollamaRequest, matchingAiModel, cancellationToken);

            _logger.LogInformation("=== Received AI Response ===");
            _logger.LogInformation("ChatId: {ChatId}, Response Length: {Length} chars", chatId, aiResponse.Length);
            
            // Log response preview only at Debug level to avoid exposing sensitive content
            if (aiResponse.Length > 0 && aiResponse.Length <= 500)
            {
                _logger.LogDebug("Response Preview: {ResponsePreview}", aiResponse);
            }
            else if (aiResponse.Length > 500)
            {
                _logger.LogDebug("Response Preview (first 500 chars): {ResponsePreview}", aiResponse.Substring(0, 500));
            }

            // Step 14-15: Extract content and create new message data from the responsed message
            var aiMessage = new ChatMessage(session)
            {
                Sender = "assistant",
                Message = aiResponse,
                Node = node,
                User = chatMessage.User,
                Timestamp = DateTime.UtcNow,
                ConnectionId = chatMessage.ConnectionId
            };

            // Step 16: Save new message data to database to get new chatId
            session.Save(aiMessage);
            await session.CommitChangesAsync(cancellationToken);

            _logger.LogInformation("Saved AI response: NewChatId={NewChatId} for original chatId {ChatId}", 
                aiMessage.Oid, chatId);

            // Step 17: Update memory - add user message to history and queue rolling summarization (non-blocking)
            await _memoryService.AddToHistoryAsync(nodeId, new HistoryMessage
            {
                Role = "user",
                Content = userMessage,
                Timestamp = chatMessage.Timestamp
            });

            // Queue rolling summarization for user message (runs in background, doesn't block chat flow)
            _memoryService.QueueSummarization(nodeId, userMessage, "user");

            _logger.LogInformation("Updated memory with user message for node {NodeId}", nodeId);

            // Step 18: Update memory - add AI message to history and queue rolling summarization (non-blocking)
            await _memoryService.AddToHistoryAsync(nodeId, new HistoryMessage
            {
                Role = "assistant",
                Content = aiResponse,
                Timestamp = aiMessage.Timestamp
            });

            // Queue rolling summarization for AI message (runs in background, doesn't block chat flow)
            _memoryService.QueueSummarization(nodeId, aiResponse, "assistant");

            _logger.LogInformation("Updated memory with AI response for node {NodeId}", nodeId);

            // Step 19: Prepare new Redis data (data B) with new chatId only
            // Other data (connectionId, nodeId, userId, projectId) are already saved in the ChatMessage
            var resultEnvelope = new Dictionary<string, string>
            {
                { "chatId", aiMessage.Oid.ToString() }
            };

            var entryId = await _redisService.Add("signalr:updates", resultEnvelope);

            _logger.LogInformation("=== Published Result to SignalR ===");
            _logger.LogInformation("EntryId: {EntryId}, NewChatId: {NewChatId}, OriginalChatId: {OriginalChatId}", 
                entryId, aiMessage.Oid, chatId);

            // Step 20: Acknowledge the Redis data (data A) - handled by returning true
            _logger.LogInformation("=== Chat Job Completed Successfully ===");
            _logger.LogInformation("Original ChatId: {ChatId}, New ChatId: {NewChatId}, NodeId: {NodeId}", 
                chatId, aiMessage.Oid, nodeId);
            
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
