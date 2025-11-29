using NodPT.Data.Models;

namespace NodPT.Data.Interfaces
{
    public interface IRedisService
    {
        /// <summary>
        /// Add a message to a Redis Stream
        /// </summary>
        Task<string> Add(string streamKey, IDictionary<string, string> envelope);

        /// <summary>
        /// Listen to a Redis Stream with consumer group
        /// </summary>
        ListenHandle Listen(string streamKey, string group, string consumerName,
            Func<MessageEnvelope, CancellationToken, Task<bool>> handler, ListenOptions? options = null);

        /// <summary>
        /// Delete (acknowledge) a message from a stream
        /// </summary>
        Task Acknowledge(string streamKey, string group, string entryId);

        /// <summary>
        /// Claim pending messages that are idle for too long
        /// </summary>
        Task<int> ClaimPending(string streamKey, string group, string consumerName, int idleThresholdMs);

        /// <summary>
        /// Trim a stream to a maximum length
        /// </summary>
        Task Trim(string streamKey, long maxLen);

        /// <summary>
        /// Get information about a stream
        /// </summary>
        Task<RedisStreamInfo> Info(string streamKey, string? group = null);

        /// <summary>
        /// Stop listening to a stream
        /// </summary>
        Task StopListen(ListenHandle handle);

        // ============================================================
        // Key-Value Operations for Memory (Summary and History)
        // ============================================================

        /// <summary>
        /// Get a string value from Redis
        /// </summary>
        Task<string?> Get(string key);

        /// <summary>
        /// Set a string value in Redis
        /// </summary>
        Task Set(string key, string value, TimeSpan? expiry = null);

        /// <summary>
        /// Check if a key exists in Redis
        /// </summary>
        Task<bool> Exists(string key);

        /// <summary>
        /// Delete a key from Redis
        /// </summary>
        Task<bool> Remove(string key);

        /// <summary>
        /// Push a value to the right of a list
        /// </summary>
        Task<long> Update(string key, string value);

        /// <summary>
        /// Get a range of values from a list
        /// </summary>
        Task<List<string>> Range(string key, long start = 0, long stop = -1);

        /// <summary>
        /// Trim a list to the specified range
        /// </summary>
        Task TrimList(string key, long start, long stop);

        /// <summary>
        /// Get the length of a list
        /// </summary>
        Task<long> Length(string key);
    }
}
