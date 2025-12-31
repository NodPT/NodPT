using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace RedisService.Cache;

/// <summary>
/// Redis Cache service providing key-value and list storage operations.
/// 
/// Used for caching data like:
/// - Conversation summaries (key-value)
/// - Chat history (ordered lists)
/// - Session data with expiration
/// 
/// All operations are simple read/write without the complexity of consumer groups.
/// </summary>
/// <example>
/// <code>
/// // Caching a summary
/// await cacheService.Set("node:summary:abc123", "This is a summary...");
/// var summary = await cacheService.Get("node:summary:abc123");
/// 
/// // Managing history list
/// await cacheService.Update("node:history:abc123", jsonMessage);
/// var messages = await cacheService.Range("node:history:abc123", -10, -1);
/// </code>
/// </example>
public class RedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    /// <summary>
    /// Initializes a new instance of the RedisCacheService.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when redis or logger is null.</exception>
    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ============================================================
    // Key-Value Operations - Simple String Storage
    // ============================================================

    /// <summary>
    /// Gets a string value from Redis by key.
    /// 
    /// Used for retrieving cached values like conversation summaries.
    /// Returns null if the key doesn't exist.
    /// </summary>
    /// <param name="key">The Redis key (e.g., "node:summary:abc123").</param>
    /// <returns>The stored string value, or null if not found.</returns>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Get a cached summary for a node
    /// var summary = await cacheService.Get("node:summary:abc123");
    /// 
    /// if (summary != null)
    /// {
    ///     Console.WriteLine($"Found cached summary: {summary}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("No cached summary found");
    /// }
    /// </code>
    /// </example>
    public async Task<string?> Get(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            
            if (value.IsNull)
            {
                _logger.LogDebug("Key {Key} not found in Redis", key);
                return null;
            }
            
            return value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting string from Redis: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Sets a string value in Redis with an optional expiration time.
    /// 
    /// Used for caching values like conversation summaries. If the key already exists,
    /// the value is overwritten.
    /// </summary>
    /// <param name="key">The Redis key (e.g., "node:summary:abc123").</param>
    /// <param name="value">The string value to store.</param>
    /// <param name="expiry">Optional expiration time. If null, the key never expires.</param>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Cache a summary with no expiration
    /// await cacheService.Set("node:summary:abc123", "This is a conversation about...");
    /// 
    /// // Cache a value that expires in 1 hour
    /// await cacheService.Set("temp:session:xyz", "session-data", TimeSpan.FromHours(1));
    /// </code>
    /// </example>
    public async Task Set(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(key, value, expiry);
            
            _logger.LogDebug("Set string in Redis: {Key} = {ValueLength} chars", key, value.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting string in Redis: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Checks if a key exists in Redis.
    /// </summary>
    /// <param name="key">The Redis key to check.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// if (await cacheService.Exists("node:summary:abc123"))
    /// {
    ///     Console.WriteLine("Summary is cached");
    /// }
    /// </code>
    /// </example>
    public async Task<bool> Exists(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key existence in Redis: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Deletes a key from Redis.
    /// </summary>
    /// <param name="key">The Redis key to delete.</param>
    /// <returns>True if the key was deleted, false if it didn't exist.</returns>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Clear a cached summary
    /// var deleted = await cacheService.Remove("node:summary:abc123");
    /// Console.WriteLine(deleted ? "Cache cleared" : "Key didn't exist");
    /// </code>
    /// </example>
    public async Task<bool> Remove(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key from Redis: {Key}", key);
            throw;
        }
    }

    // ============================================================
    // List Operations - Ordered Collection Storage
    // ============================================================

    /// <summary>
    /// Appends a value to the right (end) of a Redis List.
    /// 
    /// Used for maintaining ordered collections like chat history. Each new message
    /// is appended to the end of the list.
    /// 
    /// <para>
    /// <b>Note:</b> The method is named "Update" for historical reasons but actually
    /// performs a RPUSH (right push) operation. Consider using with <see cref="TrimList"/>
    /// to limit list size.
    /// </para>
    /// </summary>
    /// <param name="key">The Redis List key (e.g., "node:history:abc123").</param>
    /// <param name="value">The value to append (typically JSON-serialized message).</param>
    /// <returns>The new length of the list after the push.</returns>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Add a message to chat history
    /// var historyKey = "node:history:abc123";
    /// var messageJson = JsonSerializer.Serialize(new { role = "user", content = "Hello" });
    /// 
    /// var newLength = await cacheService.Update(historyKey, messageJson);
    /// Console.WriteLine($"History now has {newLength} messages");
    /// 
    /// // Trim to keep only the last 20 messages
    /// if (newLength > 20)
    /// {
    ///     await cacheService.TrimList(historyKey, -20, -1);
    /// }
    /// </code>
    /// </example>
    public async Task<long> Update(string key, string value)
    {
        try
        {
            var db = _redis.GetDatabase();
            var length = await db.ListRightPushAsync(key, value);
            
            _logger.LogDebug("Pushed to list in Redis: {Key}, new length: {Length}", key, length);
            return length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing to list in Redis: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets a range of values from a Redis List.
    /// 
    /// Supports negative indices where -1 is the last element, -2 is second to last, etc.
    /// Use start=0 and stop=-1 to get all elements.
    /// </summary>
    /// <param name="key">The Redis List key.</param>
    /// <param name="start">Start index (0-based, supports negative).</param>
    /// <param name="stop">Stop index (inclusive, supports negative). Use -1 for end of list.</param>
    /// <returns>List of string values in the specified range.</returns>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Get all messages in history
    /// var allMessages = await cacheService.Range("node:history:abc123");
    /// 
    /// // Get only the last 10 messages
    /// var recentMessages = await cacheService.Range("node:history:abc123", -10, -1);
    /// 
    /// // Get first 5 messages
    /// var firstMessages = await cacheService.Range("node:history:abc123", 0, 4);
    /// </code>
    /// </example>
    public async Task<List<string>> Range(string key, long start = 0, long stop = -1)
    {
        try
        {
            var db = _redis.GetDatabase();
            var values = await db.ListRangeAsync(key, start, stop);
            
            return values.Select(v => v.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting list range from Redis: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Trims a Redis List to only contain elements in the specified range.
    /// 
    /// Elements outside the range are deleted. Supports negative indices.
    /// Commonly used to limit list size by keeping only the most recent N elements.
    /// </summary>
    /// <param name="key">The Redis List key.</param>
    /// <param name="start">Start index to keep (0-based, supports negative).</param>
    /// <param name="stop">Stop index to keep (inclusive, supports negative).</param>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Keep only the last 20 messages in history
    /// await cacheService.TrimList("node:history:abc123", -20, -1);
    /// 
    /// // Keep only first 10 messages
    /// await cacheService.TrimList("node:history:abc123", 0, 9);
    /// </code>
    /// </example>
    public async Task TrimList(string key, long start, long stop)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.ListTrimAsync(key, start, stop);
            
            _logger.LogDebug("Trimmed list in Redis: {Key} to range [{Start}, {Stop}]", key, start, stop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trimming list in Redis: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets the length (number of elements) of a Redis List.
    /// </summary>
    /// <param name="key">The Redis List key.</param>
    /// <returns>The number of elements in the list, or 0 if the key doesn't exist.</returns>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// var historyLength = await cacheService.Length("node:history:abc123");
    /// Console.WriteLine($"Chat history has {historyLength} messages");
    /// </code>
    /// </example>
    public async Task<long> Length(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.ListLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting list length from Redis: {Key}", key);
            throw;
        }
    }
}
