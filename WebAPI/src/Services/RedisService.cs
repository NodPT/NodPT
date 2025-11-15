using StackExchange.Redis;
using System.Text.Json;

namespace NodPT.API.Services
{
    public interface IRedisService
    {
        Task ListRightPushAsync(string key, string value);
        Task PublishAsync(string channel, string message);
    }

    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisService> _logger;

        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task ListRightPushAsync(string key, string value)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.ListRightPushAsync(key, value);
                _logger.LogInformation($"Pushed message to Redis list: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pushing to Redis list: {key}");
                throw;
            }
        }

        public async Task PublishAsync(string channel, string message)
        {
            try
            {
                var subscriber = _redis.GetSubscriber();
                await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
                _logger.LogInformation($"Published message to Redis channel: {channel}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing to Redis channel: {channel}");
                throw;
            }
        }
    }
}
