using BackendExecutor.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace BackendExecutor.Consumers;

public interface IChatJobConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
}

public class ChatJobConsumer : IChatJobConsumer
{
    private readonly ILogger<ChatJobConsumer> _logger;
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILlmChatService _llmChatService;

    public ChatJobConsumer(
        ILogger<ChatJobConsumer> logger,
        IDatabase database,
        IConnectionMultiplexer redis,
        ILlmChatService llmChatService)
    {
        _logger = logger;
        _database = database;
        _redis = redis;
        _llmChatService = llmChatService;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ChatJobConsumer: Starting to consume from chat.jobs list");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Pop message from the left of the list (FIFO queue)
                var message = await _database.ListLeftPopAsync("chat.jobs");

                if (message.HasValue)
                {
                    await ProcessChatJob(message.ToString(), cancellationToken);
                }
                else
                {
                    // No messages, wait a bit before polling again
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ChatJobConsumer: Stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatJobConsumer: Error while consuming messages");
                await Task.Delay(5000, cancellationToken); // Wait before retrying
            }
        }
    }

    private async Task ProcessChatJob(string messageJson, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"ChatJobConsumer: Processing chat job: {messageJson}");

            var chatJob = JsonSerializer.Deserialize<ChatJobDto>(messageJson);
            if (chatJob == null)
            {
                _logger.LogWarning("Failed to deserialize chat job");
                return;
            }

            // Extract model name from NodeLevel
            var modelName = ExtractModelName(chatJob.NodeLevel);
            _logger.LogInformation($"ChatJobConsumer: Using model: {modelName}");

            // Send request to LLM endpoint
            var aiResponse = await SendToLlm(chatJob, modelName, cancellationToken);

            // Publish response to Redis channel AI.RESPONSE
            var responseDto = new
            {
                ConnectionId = chatJob.ConnectionId,
                Content = aiResponse
            };

            var responseJson = JsonSerializer.Serialize(responseDto);
            var subscriber = _redis.GetSubscriber();
            await subscriber.PublishAsync(RedisChannel.Literal("AI.RESPONSE"), responseJson);

            _logger.LogInformation($"ChatJobConsumer: Published AI response to Redis channel for connection {chatJob.ConnectionId}");
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "ChatJobConsumer: JSON deserialization failed for chat job: {MessageJson}", messageJson);
        }
        catch (RedisException redisEx)
        {
            _logger.LogError(redisEx, "ChatJobConsumer: Redis publish failed for chat job: {MessageJson}", messageJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatJobConsumer: Unexpected error while processing chat job: {MessageJson}", messageJson);
        }
    }

    private string ExtractModelName(string? nodeLevel)
    {
        // Extract model name from NodeLevel
        // NodeLevel format could be like "manager", "inspector", "agent", or specific model names
        if (string.IsNullOrEmpty(nodeLevel))
        {
            return "default-model";
        }

        // Map node levels to model names
        return nodeLevel.ToLower() switch
        {
            "manager" => "trt-llm-manager",
            "inspector" => "trt-llm-inspector",
            "agent" => "trt-llm-agent",
            _ => nodeLevel
        };
    }

    private async Task<string> SendToLlm(ChatJobDto chatJob, string modelName, CancellationToken cancellationToken)
    {
        try
        {
            // Use the LlmChatService to send the message
            var response = await _llmChatService.SendChatMessageAsync(
                chatJob.Message ?? string.Empty,
                modelName,
                maxTokens: 64,
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
}

public class ChatJobDto
{
    public string? UserId { get; set; }
    public string? ConnectionId { get; set; }
    public string? Message { get; set; }
    public string? ProjectId { get; set; }
    public string? NodeLevel { get; set; }
}
