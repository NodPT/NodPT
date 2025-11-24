using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace NodPT.Data.Services;

public interface IRedisService
{
    Task PublishAsync(string channel, string message);
    Task ListRightPushAsync(string key, string value);
    Task<RedisValue> ListLeftPopAsync(string key);
    Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler);
}

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(string channel, string message)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
            _logger?.LogInformation($"Published message to Redis channel: {channel}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error publishing to Redis channel: {channel}");
            throw;
        }
    }

    public async Task ListRightPushAsync(string key, string value)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.ListRightPushAsync(key, value);
            _logger?.LogInformation($"Pushed message to Redis list: {key}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error pushing to Redis list: {key}");
            throw;
        }
    }

    public async Task<RedisValue> ListLeftPopAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.ListLeftPopAsync(key);
            if (value.HasValue)
            {
                _logger?.LogInformation($"Popped message from Redis list: {key}");
            }
            return value;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error popping from Redis list: {key}");
            throw;
        }
    }

    public async Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            await subscriber.SubscribeAsync(RedisChannel.Literal(channel), handler);
            _logger?.LogInformation($"Subscribed to Redis channel: {channel}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error subscribing to Redis channel: {channel}");
            throw;
        }
    }
}
