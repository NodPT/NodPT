using NodPT.Data.Models;

namespace NodPT.Data.Interfaces
{
    /// <summary>
    /// Interface for Redis Queue operations using Redis Streams.
    /// 
    /// Redis Streams provide reliable message queuing between services with features like:
    /// - Consumer groups for load balancing
    /// - Message acknowledgment for at-least-once delivery
    /// - Dead letter handling for failed messages
    /// - Pending message recovery
    /// 
    /// Used for async communication between WebAPI → Executor → SignalR.
    /// </summary>
    /// <example>
    /// <code>
    /// // Publishing a message to a queue
    /// var envelope = new Dictionary&lt;string, string&gt; { { "chatId", "12345" } };
    /// var entryId = await queueService.Add("jobs:chat", envelope);
    /// 
    /// // Consuming messages from a queue
    /// var handle = queueService.Listen("jobs:chat", "executor", "worker-1", 
    ///     async (msg, ct) => { /* process */ return true; });
    /// </code>
    /// </example>
    public interface IRedisQueueService
    {
        /// <summary>
        /// Adds a message to a Redis Stream for asynchronous processing by consumers.
        /// 
        /// Each message gets a unique entry ID (timestamp-based) and can be consumed by 
        /// multiple consumer groups.
        /// </summary>
        /// <param name="streamKey">The key/name of the Redis Stream (e.g., "jobs:chat", "signalr:updates").</param>
        /// <param name="envelope">Dictionary of field-value pairs to include in the message.</param>
        /// <returns>The unique entry ID assigned to the message.</returns>
        /// <example>
        /// <code>
        /// var envelope = new Dictionary&lt;string, string&gt; { { "chatId", "12345" } };
        /// var entryId = await queueService.Add("jobs:chat", envelope);
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
    }
}
