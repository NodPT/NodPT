namespace NodPT.Data.Interfaces
{
    /// <summary>
    /// Interface for Redis Cache operations providing key-value and list storage.
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
    public interface IRedisCacheService
    {
        // ============================================================
        // Key-Value Operations - Simple String Storage
        // ============================================================

        /// <summary>
        /// Gets a string value from Redis by key.
        /// </summary>
        /// <param name="key">The Redis key.</param>
        /// <returns>The stored string value, or null if not found.</returns>
        /// <example>
        /// <code>
        /// var summary = await cacheService.Get("node:summary:abc123");
        /// if (summary != null) Console.WriteLine($"Found: {summary}");
        /// </code>
        /// </example>
        Task<string?> Get(string key);

        /// <summary>
        /// Sets a string value in Redis with an optional expiration time.
        /// </summary>
        /// <param name="key">The Redis key.</param>
        /// <param name="value">The string value to store.</param>
        /// <param name="expiry">Optional expiration time.</param>
        /// <example>
        /// <code>
        /// // Cache with no expiration
        /// await cacheService.Set("node:summary:abc123", "This is a summary...");
        /// 
        /// // Cache with 1 hour expiration
        /// await cacheService.Set("session:xyz", "data", TimeSpan.FromHours(1));
        /// </code>
        /// </example>
        Task Set(string key, string value, TimeSpan? expiry = null);

        /// <summary>
        /// Checks if a key exists in Redis.
        /// </summary>
        /// <param name="key">The Redis key to check.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        Task<bool> Exists(string key);

        /// <summary>
        /// Deletes a key from Redis.
        /// </summary>
        /// <param name="key">The Redis key to delete.</param>
        /// <returns>True if the key was deleted, false if it didn't exist.</returns>
        Task<bool> Remove(string key);

        // ============================================================
        // List Operations - Ordered Collection Storage
        // ============================================================

        /// <summary>
        /// Appends a value to the right (end) of a Redis List.
        /// Used for maintaining ordered collections like chat history.
        /// </summary>
        /// <param name="key">The Redis List key.</param>
        /// <param name="value">The value to append.</param>
        /// <returns>The new length of the list after the push.</returns>
        /// <example>
        /// <code>
        /// var historyKey = "node:history:abc123";
        /// var newLength = await cacheService.Update(historyKey, jsonMessage);
        /// 
        /// // Trim to keep only last 20 messages
        /// if (newLength > 20)
        ///     await cacheService.TrimList(historyKey, -20, -1);
        /// </code>
        /// </example>
        Task<long> Update(string key, string value);

        /// <summary>
        /// Gets a range of values from a Redis List.
        /// Supports negative indices (-1 is last element).
        /// </summary>
        /// <param name="key">The Redis List key.</param>
        /// <param name="start">Start index (0-based, supports negative).</param>
        /// <param name="stop">Stop index (inclusive, supports negative).</param>
        /// <returns>List of string values in the specified range.</returns>
        /// <example>
        /// <code>
        /// // Get all messages
        /// var all = await cacheService.Range("node:history:abc123");
        /// 
        /// // Get last 10 messages
        /// var recent = await cacheService.Range("node:history:abc123", -10, -1);
        /// </code>
        /// </example>
        Task<List<string>> Range(string key, long start = 0, long stop = -1);

        /// <summary>
        /// Trims a Redis List to only contain elements in the specified range.
        /// </summary>
        /// <param name="key">The Redis List key.</param>
        /// <param name="start">Start index to keep.</param>
        /// <param name="stop">Stop index to keep (inclusive).</param>
        /// <example>
        /// <code>
        /// // Keep only the last 20 messages
        /// await cacheService.TrimList("node:history:abc123", -20, -1);
        /// </code>
        /// </example>
        Task TrimList(string key, long start, long stop);

        /// <summary>
        /// Gets the length (number of elements) of a Redis List.
        /// </summary>
        /// <param name="key">The Redis List key.</param>
        /// <returns>The number of elements in the list.</returns>
        Task<long> Length(string key);
    }
}
