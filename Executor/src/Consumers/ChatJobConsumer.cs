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

    public ChatJobConsumer(
        ILogger<ChatJobConsumer> logger,
        IDatabase database,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _database = database;
        _redis = redis;
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

            // TODO: Send request to TensorRT-LLM container
            // For now, simulate AI processing
            var aiResponse = await SimulateAiProcessing(chatJob, modelName, cancellationToken);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatJobConsumer: Failed to process chat job");
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

    private async Task<string> SimulateAiProcessing(ChatJobDto chatJob, string modelName, CancellationToken cancellationToken)
    {
        // Simulate AI processing delay
        await Task.Delay(2000, cancellationToken);

        // Generate a mock AI response
        // TODO: Replace this with actual TensorRT-LLM call
        var responses = new[]
        {
            $"[{modelName}] I've analyzed your request: '{chatJob.Message}'. Here's my response based on the context of project {chatJob.ProjectId}.",
            $"[{modelName}] Thank you for your message. I understand you're working on {chatJob.ProjectId}. Let me help you with that.",
            $"[{modelName}] Based on your input, I recommend the following approach for your workflow in project {chatJob.ProjectId}.",
            $"[{modelName}] I've processed your request. Here's a comprehensive solution tailored to your needs."
        };

        var random = new Random();
        return responses[random.Next(responses.Length)];
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
