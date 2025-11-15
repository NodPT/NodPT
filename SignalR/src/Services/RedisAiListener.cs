using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;
using NodPT.SignalR.Hubs;

namespace NodPT.SignalR.Services;

public class RedisAiListener : BackgroundService
{
    private readonly IHubContext<NodptHub> _hubContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisAiListener> _logger;

    public RedisAiListener(
        IHubContext<NodptHub> hubContext,
        IConnectionMultiplexer redis,
        ILogger<RedisAiListener> logger)
    {
        _hubContext = hubContext;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RedisAiListener starting...");

        var subscriber = _redis.GetSubscriber();

        // Subscribe to AI.RESPONSE channel
        await subscriber.SubscribeAsync(RedisChannel.Literal("AI.RESPONSE"), async (channel, message) =>
        {
            try
            {
                _logger.LogInformation($"Received AI response from Redis channel: {channel}");

                // Parse the message
                var responseDto = JsonSerializer.Deserialize<AiResponseDto>(message.ToString());

                if (responseDto == null || string.IsNullOrEmpty(responseDto.ConnectionId))
                {
                    _logger.LogWarning("Received invalid AI response: missing ConnectionId");
                    return;
                }

                // Send to specific client via SignalR
                await _hubContext.Clients.Client(responseDto.ConnectionId)
                    .SendAsync("ReceiveAIResponse", new
                    {
                        content = responseDto.Content,
                        timestamp = DateTime.UtcNow
                    }, stoppingToken);

                _logger.LogInformation($"AI response sent to client: {responseDto.ConnectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI response from Redis");
            }
        });

        _logger.LogInformation("RedisAiListener subscribed to AI.RESPONSE channel");

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("RedisAiListener stopped");
    }
}

public class AiResponseDto
{
    public string? ConnectionId { get; set; }
    public string? Content { get; set; }
}
