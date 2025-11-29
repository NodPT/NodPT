using NodPT.Data.Models;

namespace NodPT.Data.Interfaces
{
    /// <summary>
    /// Interface for Redis operations providing three categories of functionality:
    /// 
    /// <list type="number">
    /// <item>
    /// <term>Redis Streams</term>
    /// <description>
    /// Message queuing between services (Add, Listen, Acknowledge, ClaimPending, Trim, Info, StopListen).
    /// Used for reliable async communication between WebAPI → Executor → SignalR.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Key-Value Operations</term>
    /// <description>
    /// Simple string storage (Get, Set, Exists, Remove).
    /// Used for caching conversation summaries.
    /// </description>
    /// </item>
    /// <item>
    /// <term>List Operations</term>
    /// <description>
    /// Ordered collection storage (Update, Range, TrimList, Length).
    /// Used for storing chat history.
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    public interface IRedisService
    {
        // ============================================================
        // Redis Stream Operations - Message Queue Functionality
        // ============================================================

        /// <summary>
        /// Adds a message to a Redis Stream for asynchronous processing by consumers.
        /// 
        /// <para>
        /// <b>Note:</b> This is different from <see cref="Set"/> which stores a simple key-value pair.
        /// Use <c>Add</c> for message queuing, use <c>Set</c> for caching single values.
        /// </para>
        /// </summary>
        /// <param name="streamKey">The key/name of the Redis Stream (e.g., "jobs:chat", "signalr:updates").</param>
        /// <param name="envelope">Dictionary of field-value pairs to include in the message.</param>
        /// <returns>The unique entry ID assigned to the message.</returns>
        /// <example>
        /// <code>
        /// var envelope = new Dictionary&lt;string, string&gt; { { "chatId", "12345" } };
        /// var entryId = await redisService.Add("jobs:chat", envelope);
        /// </code>
        /// </example>
        Task<string> Add(string streamKey, IDictionary<string, string> envelope);

        /// <summary>
        /// Starts a background listener for a Redis Stream using consumer groups.
        /// Messages must be acknowledged after successful processing.
        /// </summary>
        /// <param name="streamKey">The Redis Stream key to listen to.</param>
        /// <param name="group">The consumer group name.</param>
        /// <param name="consumerName">Unique name for this consumer instance.</param>
        /// <param name="handler">Async callback for each message. Return true to acknowledge.</param>
        /// <param name="options">Optional configuration for batch size, concurrency, retries.</param>
        /// <returns>A handle to control the listener (use with <see cref="StopListen"/>).</returns>
        ListenHandle Listen(string streamKey, string group, string consumerName,
            Func<MessageEnvelope, CancellationToken, Task<bool>> handler, ListenOptions? options = null);

        /// <summary>
        /// Acknowledges a message in a Redis Stream, marking it as successfully processed.
        /// </summary>
        /// <param name="streamKey">The Redis Stream key.</param>
        /// <param name="group">The consumer group name.</param>
        /// <param name="entryId">The entry ID of the message to acknowledge.</param>
        Task Acknowledge(string streamKey, string group, string entryId);

        /// <summary>
        /// Claims pending messages that have been idle (unacknowledged) for too long.
        /// Used to recover from consumer failures.
        /// </summary>
        /// <param name="streamKey">The Redis Stream key.</param>
        /// <param name="group">The consumer group name.</param>
        /// <param name="consumerName">The name of the consumer claiming the messages.</param>
        /// <param name="idleThresholdMs">Minimum idle time in milliseconds before a message can be claimed.</param>
        /// <returns>The number of messages successfully claimed.</returns>
        Task<int> ClaimPending(string streamKey, string group, string consumerName, int idleThresholdMs);

        /// <summary>
        /// Trims a Redis Stream to approximately the specified maximum length.
        /// 
        /// <para>
        /// <b>Note:</b> This is for Redis Streams. For Redis Lists, use <see cref="TrimList"/>.
        /// </para>
        /// </summary>
        /// <param name="streamKey">The Redis Stream key to trim.</param>
        /// <param name="maxLen">The approximate maximum number of entries to keep.</param>
        Task Trim(string streamKey, long maxLen);

        /// <summary>
        /// Gets information about a Redis Stream including length and pending message counts.
        /// </summary>
        /// <param name="streamKey">The Redis Stream key.</param>
        /// <param name="group">Optional consumer group name to get pending message info.</param>
        /// <returns>Stream information including length and per-consumer pending counts.</returns>
        Task<RedisStreamInfo> Info(string streamKey, string? group = null);

        /// <summary>
        /// Stops a stream listener that was started with <see cref="Listen"/>.
        /// </summary>
        /// <param name="handle">The listen handle returned by <see cref="Listen"/>.</param>
        Task StopListen(ListenHandle handle);

        // ============================================================
        // Key-Value Operations - Simple String Storage
        // ============================================================

        /// <summary>
        /// Gets a string value from Redis by key.
        /// </summary>
        /// <param name="key">The Redis key.</param>
        /// <returns>The stored string value, or null if not found.</returns>
        Task<string?> Get(string key);

        /// <summary>
        /// Sets a string value in Redis with an optional expiration time.
        /// 
        /// <para>
        /// <b>Note:</b> This is different from <see cref="Add"/> which appends to a Redis Stream.
        /// Use <c>Set</c> for caching single values, use <c>Add</c> for message queuing.
        /// </para>
        /// </summary>
        /// <param name="key">The Redis key.</param>
        /// <param name="value">The string value to store.</param>
        /// <param name="expiry">Optional expiration time.</param>
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
        Task<long> Update(string key, string value);

        /// <summary>
        /// Gets a range of values from a Redis List.
        /// Supports negative indices (-1 is last element).
        /// </summary>
        /// <param name="key">The Redis List key.</param>
        /// <param name="start">Start index (0-based, supports negative).</param>
        /// <param name="stop">Stop index (inclusive, supports negative).</param>
        /// <returns>List of string values in the specified range.</returns>
        Task<List<string>> Range(string key, long start = 0, long stop = -1);

        /// <summary>
        /// Trims a Redis List to only contain elements in the specified range.
        /// 
        /// <para>
        /// <b>Note:</b> This is for Redis Lists. For Redis Streams, use <see cref="Trim"/>.
        /// </para>
        /// </summary>
        /// <param name="key">The Redis List key.</param>
        /// <param name="start">Start index to keep.</param>
        /// <param name="stop">Stop index to keep (inclusive).</param>
        Task TrimList(string key, long start, long stop);

        /// <summary>
        /// Gets the length (number of elements) of a Redis List.
        /// </summary>
        /// <param name="key">The Redis List key.</param>
        /// <returns>The number of elements in the list.</returns>
        Task<long> Length(string key);
    }
}
