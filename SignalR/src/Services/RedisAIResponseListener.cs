using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;
using NodPT.SignalR.Hubs;

namespace NodPT.SignalR.Services;

public class RedisAIResponseListener : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<NodptHub> _hubContext;
    private readonly ILogger<RedisAIResponseListener> _logger;

    public RedisAIResponseListener(
        IConnectionMultiplexer redis,
        IHubContext<NodptHub> hubContext,
        ILogger<RedisAIResponseListener> logger)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RedisAIResponseListener starting...");

        var subscriber = _redis.GetSubscriber();

        // Subscribe to AI.RESPONSE channel
        await subscriber.SubscribeAsync(RedisChannel.Literal("AI.RESPONSE"), async (channel, message) =>
        {
            try
            {
                _logger.LogInformation($"Received AI response: {message}");
                
                // Parse the response
                var response = JsonSerializer.Deserialize<AIResponseDto>(message.ToString());
                
                if (response == null || string.IsNullOrEmpty(response.ConnectionId))
                {
                    _logger.LogWarning("Invalid AI response format");
                    return;
                }

                // Generate a chat ID for tracking
                var chatId = Guid.NewGuid().ToString();

                // Send the response to the specific client connection
                await _hubContext.Clients.Client(response.ConnectionId).SendAsync(
                    "ReceiveAIResponse",
                    new
                    {
                        chatId = chatId,
                        content = response.Content,
                        timestamp = DateTime.UtcNow
                    },
                    stoppingToken);

                _logger.LogInformation($"Sent AI response to client {response.ConnectionId}, chatId: {chatId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI response");
            }
        });

        _logger.LogInformation("RedisAIResponseListener is now listening to AI.RESPONSE channel");

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
        
        _logger.LogInformation("RedisAIResponseListener stopped");
    }
}

public class AIResponseDto
{
    public string? ConnectionId { get; set; }
    public string? Content { get; set; }
}
