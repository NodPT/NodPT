using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using NodPT.Data.Models;
using System.Collections.Concurrent;
using NodPT.Data.Interfaces;

namespace NodPT.Data.Services;

/// <summary>
/// Unified Redis service implementing both Queue and Cache operations.
/// 
/// This class provides a complete Redis implementation supporting:
/// 
/// <list type="bullet">
/// <item>
/// <term>Queue Operations (<see cref="IRedisQueueService"/>)</term>
/// <description>
/// Message queuing using Redis Streams: Add, Listen, Acknowledge, ClaimPending, Trim, Info, StopListen.
/// Used for reliable async communication between WebAPI → Executor → SignalR.
/// </description>
/// </item>
/// <item>
/// <term>Cache Operations (<see cref="IRedisCacheService"/>)</term>
/// <description>
/// Key-Value storage (Get, Set, Exists, Remove) and List operations (Update, Range, TrimList, Length).
/// Used for caching summaries and storing chat history.
/// </description>
/// </item>
/// </list>
/// 
/// The service can be injected as <see cref="IRedisService"/> (unified), 
/// <see cref="IRedisQueueService"/> (queue only), or <see cref="IRedisCacheService"/> (cache only).
/// </summary>
public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisService> _logger;
    private readonly ConcurrentDictionary<string, int> _retryCounters = new();

    /// <summary>
    /// Initializes a new instance of the RedisService.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when redis or logger is null.</exception>
    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IRedisQueueService - Message Queue Operations

    /// <summary>
    /// Adds a message to a Redis Stream for asynchronous processing by consumers.
    /// 
    /// Redis Streams are append-only logs ideal for message queuing between services.
    /// Each message gets a unique entry ID (timestamp-based) and can be consumed by 
    /// multiple consumer groups.
    /// 
    /// <para>
    /// <b>Note:</b> This is different from <see cref="Set"/> which stores a simple key-value pair.
    /// Use <c>Add</c> for message queuing, use <c>Set</c> for caching single values.
    /// </para>
    /// </summary>
    /// <param name="streamKey">The key/name of the Redis Stream (e.g., "jobs:chat", "signalr:updates").</param>
    /// <param name="envelope">Dictionary of field-value pairs to include in the message.</param>
    /// <returns>The unique entry ID assigned to the message (e.g., "1609459200000-0").</returns>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Queue a chat job for processing by the Executor service
    /// var envelope = new Dictionary&lt;string, string&gt;
    /// {
    ///     { "chatId", "12345" },
    ///     { "connectionId", "abc-123-def" }
    /// };
    /// var entryId = await redisService.Add("jobs:chat", envelope);
    /// // entryId = "1609459200000-0"
    /// </code>
    /// </example>
    public async Task<string> Add(string streamKey, IDictionary<string, string> envelope)
    {
        try
        {
            var db = _redis.GetDatabase();
            
            // Convert dictionary to NameValueEntry array
            var entries = envelope.Select(kv => new NameValueEntry(kv.Key, kv.Value)).ToArray();
            
            // Add to stream
            var entryId = await db.StreamAddAsync(streamKey, entries);
            
            _logger.LogDebug($"Added message to Redis stream {streamKey}: {entryId}");
            
            return entryId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to Redis stream: {StreamKey}", streamKey);
            throw;
        }
    }

    /// <summary>
    /// Starts a background listener for a Redis Stream using consumer groups.
    /// 
    /// Consumer groups allow multiple instances to process messages from the same stream
    /// without duplicates. Each message is delivered to exactly one consumer in the group.
    /// Messages must be acknowledged after successful processing.
    /// 
    /// Features:
    /// - Automatic consumer group creation if missing
    /// - Claims pending messages from failed consumers on startup
    /// - Configurable concurrency and batch sizes
    /// - Automatic retry with dead letter support
    /// </summary>
    /// <param name="streamKey">The Redis Stream key to listen to (e.g., "jobs:chat").</param>
    /// <param name="group">The consumer group name (e.g., "executor", "signalr").</param>
    /// <param name="consumerName">Unique name for this consumer instance (e.g., "executor-host1-abc123").</param>
    /// <param name="handler">
    /// Async callback invoked for each message. Return true to acknowledge (success),
    /// false to trigger retry. After max retries, message moves to dead letter stream.
    /// </param>
    /// <param name="options">Optional configuration for batch size, concurrency, retries, etc.</param>
    /// <returns>A handle to control the listener (use with <see cref="StopListen"/>).</returns>
    /// <example>
    /// <code>
    /// // Start listening for chat jobs in the Executor service
    /// var options = new ListenOptions
    /// {
    ///     BatchSize = 10,
    ///     Concurrency = 3,
    ///     MaxRetries = 3
    /// };
    /// 
    /// var consumerName = $"executor-{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";
    /// 
    /// var handle = redisService.Listen(
    ///     streamKey: "jobs:chat",
    ///     group: "executor",
    ///     consumerName: consumerName,
    ///     handler: async (envelope, ct) =>
    ///     {
    ///         var chatId = envelope.Fields["chatId"];
    ///         // Process the chat message...
    ///         return true; // Acknowledge on success
    ///     },
    ///     options: options);
    /// 
    /// // Later, to stop listening:
    /// await redisService.StopListen(handle);
    /// </code>
    /// </example>
    public ListenHandle Listen(string streamKey, string group, string consumerName,
        Func<MessageEnvelope, CancellationToken, Task<bool>> handler, ListenOptions? options = null)
    {
        options ??= new ListenOptions();
        
        var handle = new ListenHandle
        {
            StreamKey = streamKey,
            Group = group,
            ConsumerName = consumerName
        };

        // Start background task
        handle.BackgroundTask = Task.Run(async () =>
        {
            var db = _redis.GetDatabase();
            var cancellationToken = handle.CancellationTokenSource.Token;

            try
            {
                // Create consumer group if needed
                if (options.CreateStreamIfMissing)
                {
                    await EnsureConsumerGroupExists(db, streamKey, group);
                }

                // Claim pending messages on startup
                if (options.ClaimPendingOnStartup)
                {
                    await ClaimPending(streamKey, group, consumerName, options.ClaimIdleThresholdMs);
                }

                _logger.LogInformation($"Started listening to Redis stream {streamKey} with group {group} as {consumerName}");

                // Main processing loop
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Read messages from stream
                        var entries = await db.StreamReadGroupAsync(
                            streamKey,
                            group,
                            consumerName,
                            ">",
                            count: options.BatchSize,
                            noAck: false);

                        if (entries.Length > 0)
                        {
                            // Process messages concurrently if configured
                            var tasks = new List<Task>();
                            using (var semaphore = new SemaphoreSlim(options.Concurrency, options.Concurrency))
                            {
                                foreach (var entry in entries)
                                {
                                    await semaphore.WaitAsync(cancellationToken);
                                    
                                    var task = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await ProcessMessage(db, streamKey, group, entry, handler, options, cancellationToken);
                                        }
                                        finally
                                        {
                                            semaphore.Release();
                                        }
                                    }, cancellationToken);

                                    tasks.Add(task);
                                }

                                // Wait for all messages in this batch to complete
                                await Task.WhenAll(tasks);
                            }
                        }
                        else
                        {
                            // No messages, wait before polling again
                            await Task.Delay(options.PollDelayMs, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error reading from Redis stream {streamKey}");
                        await Task.Delay(5000, cancellationToken); // Wait before retrying
                    }
                }

                _logger.LogInformation($"Stopped listening to Redis stream {streamKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fatal error in Listen loop for stream {streamKey}");
            }
        });

        return handle;
    }

    /// <summary>
    /// Processes a single message from the stream, calling the handler and managing acknowledgment/retry.
    /// </summary>
    private async Task ProcessMessage(IDatabase db, string streamKey, string group, 
        StreamEntry entry, Func<MessageEnvelope, CancellationToken, Task<bool>> handler,
        ListenOptions options, CancellationToken cancellationToken)
    {
        var entryId = entry.Id.ToString();
        var retryKey = $"{streamKey}:{entryId}";

        try
        {
            // Parse envelope
            var envelope = new MessageEnvelope
            {
                StreamKey = streamKey,
                EntryId = entryId,
                Fields = entry.Values.ToDictionary(kv => kv.Name.ToString(), kv => kv.Value.ToString())
            };

            _logger.LogDebug($"Processing message {entryId} from stream {streamKey}");

            // Call handler
            var success = await handler(envelope, cancellationToken);

            if (success)
            {
                // Acknowledge the message
                await Acknowledge(streamKey, group, entryId);
                
                // Remove retry counter
                _retryCounters.TryRemove(retryKey, out _);
                
                _logger.LogDebug($"Successfully processed and acknowledged message {entryId}");
            }
            else
            {
                // Handler returned false, increment retry counter
                var retryCount = _retryCounters.AddOrUpdate(retryKey, 1, (k, v) => v + 1);
                
                if (retryCount >= options.MaxRetries)
                {
                    // Move to dead letter stream
                    await MoveToDeadLetter(db, streamKey, group, entry);
                    _retryCounters.TryRemove(retryKey, out _);
                    
                    _logger.LogWarning($"Message {entryId} moved to dead letter after {retryCount} retries");
                }
                else
                {
                    _logger.LogWarning($"Message {entryId} failed, retry {retryCount}/{options.MaxRetries}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message {entryId} from stream {streamKey}");
            
            // Increment retry counter
            var retryCount = _retryCounters.AddOrUpdate(retryKey, 1, (k, v) => v + 1);
            
            if (retryCount >= options.MaxRetries)
            {
                // Move to dead letter stream
                await MoveToDeadLetter(db, streamKey, group, entry);
                _retryCounters.TryRemove(retryKey, out _);
                
                _logger.LogError($"Message {entryId} moved to dead letter after {retryCount} failed attempts");
            }
        }
    }

    /// <summary>
    /// Moves a failed message to the dead letter stream for manual inspection.
    /// The dead letter stream key is the original stream key with ":dead" suffix.
    /// </summary>
    private async Task MoveToDeadLetter(IDatabase db, string streamKey, string group, StreamEntry entry)
    {
        try
        {
            var deadLetterKey = $"{streamKey}:dead";
            
            // Add original entry plus metadata to dead letter stream
            var deadLetterEntries = entry.Values.ToList();
            deadLetterEntries.Add(new NameValueEntry("original_id", entry.Id.ToString()));
            deadLetterEntries.Add(new NameValueEntry("failed_at", DateTime.UtcNow.ToString("o")));
            
            await db.StreamAddAsync(deadLetterKey, deadLetterEntries.ToArray());
            
            // Acknowledge and delete the original message
            await db.StreamAcknowledgeAsync(streamKey, group, entry.Id);
            await db.StreamDeleteAsync(streamKey, new[] { entry.Id });
            
            _logger.LogInformation($"Moved message {entry.Id} to dead letter stream {deadLetterKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error moving message to dead letter stream");
        }
    }

    /// <summary>
    /// Acknowledges a message in a Redis Stream, marking it as successfully processed.
    /// 
    /// Acknowledged messages are removed from the consumer's pending list but remain
    /// in the stream until explicitly trimmed. This enables reliable message delivery
    /// with at-least-once semantics.
    /// </summary>
    /// <param name="streamKey">The Redis Stream key.</param>
    /// <param name="group">The consumer group name.</param>
    /// <param name="entryId">The entry ID of the message to acknowledge.</param>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Manually acknowledge a message after successful processing
    /// await redisService.Acknowledge("jobs:chat", "executor", "1609459200000-0");
    /// </code>
    /// </example>
    public async Task Acknowledge(string streamKey, string group, string entryId)
    {
        try
        {
            var db = _redis.GetDatabase();
            
            // Acknowledge the message
            await db.StreamAcknowledgeAsync(streamKey, group, entryId);
            
            _logger.LogDebug($"Acknowledged message {entryId} in stream {streamKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging message {EntryId} in stream {StreamKey}", entryId, streamKey);
            throw;
        }
    }

    /// <summary>
    /// Claims pending messages that have been idle (unacknowledged) for too long.
    /// 
    /// When a consumer crashes or fails to acknowledge messages, those messages remain
    /// in the pending entries list (PEL). This method allows other consumers to claim
    /// ownership of those messages and retry processing them.
    /// 
    /// This is typically called on consumer startup to recover from previous failures.
    /// </summary>
    /// <param name="streamKey">The Redis Stream key.</param>
    /// <param name="group">The consumer group name.</param>
    /// <param name="consumerName">The name of the consumer claiming the messages.</param>
    /// <param name="idleThresholdMs">Minimum idle time in milliseconds before a message can be claimed.</param>
    /// <returns>The number of messages successfully claimed.</returns>
    /// <example>
    /// <code>
    /// // Claim messages that have been pending for more than 60 seconds
    /// var claimedCount = await redisService.ClaimPending(
    ///     "jobs:chat", 
    ///     "executor", 
    ///     "executor-host1-abc123", 
    ///     idleThresholdMs: 60000);
    /// 
    /// Console.WriteLine($"Claimed {claimedCount} pending messages");
    /// </code>
    /// </example>
    public async Task<int> ClaimPending(string streamKey, string group, string consumerName, int idleThresholdMs)
    {
        try
        {
            var db = _redis.GetDatabase();
            
            // Get pending messages
            var pendingInfo = await db.StreamPendingAsync(streamKey, group);
            
            if (pendingInfo.PendingMessageCount == 0)
            {
                return 0;
            }

            // Get detailed pending messages
            var pendingMessages = await db.StreamPendingMessagesAsync(
                streamKey, 
                group, 
                count: 100, 
                consumerName: RedisValue.Null);

            var claimedCount = 0;

            foreach (var pending in pendingMessages)
            {
                // Check if message is idle for long enough
                if (pending.IdleTimeInMilliseconds >= idleThresholdMs)
                {
                    try
                    {
                        // Claim the message
                        var claimed = await db.StreamClaimAsync(
                            streamKey,
                            group,
                            consumerName,
                            minIdleTimeInMs: idleThresholdMs,
                            messageIds: new[] { pending.MessageId });

                        if (claimed.Length > 0)
                        {
                            claimedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to claim message {pending.MessageId}");
                    }
                }
            }

            if (claimedCount > 0)
            {
                _logger.LogInformation($"Claimed {claimedCount} pending messages in stream {streamKey}");
            }

            return claimedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error claiming pending messages in stream {StreamKey}", streamKey);
            return 0;
        }
    }

    /// <summary>
    /// Trims a Redis Stream to approximately the specified maximum length.
    /// 
    /// Redis uses an approximate trimming strategy (~) for performance, which may leave
    /// slightly more entries than specified. This helps manage stream size and memory usage.
    /// 
    /// <para>
    /// <b>Note:</b> This is for Redis Streams. For Redis Lists, use <see cref="TrimList"/>.
    /// </para>
    /// </summary>
    /// <param name="streamKey">The Redis Stream key to trim.</param>
    /// <param name="maxLen">The approximate maximum number of entries to keep.</param>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Keep approximately the last 10,000 messages in the stream
    /// await redisService.Trim("jobs:chat", maxLen: 10000);
    /// </code>
    /// </example>
    public async Task Trim(string streamKey, long maxLen)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.StreamTrimAsync(streamKey, maxLen, useApproximateMaxLength: true);
            
            _logger.LogDebug($"Trimmed stream {streamKey} to approximately {maxLen} messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trimming stream {StreamKey}", streamKey);
            throw;
        }
    }

    /// <summary>
    /// Gets information about a Redis Stream including length and pending message counts.
    /// 
    /// Useful for monitoring stream health, identifying stuck consumers, and debugging
    /// message processing issues.
    /// </summary>
    /// <param name="streamKey">The Redis Stream key.</param>
    /// <param name="group">Optional consumer group name to get pending message info.</param>
    /// <returns>Stream information including length and per-consumer pending counts.</returns>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Get stream info with pending message counts per consumer
    /// var info = await redisService.Info("jobs:chat", group: "executor");
    /// 
    /// Console.WriteLine($"Stream length: {info.Length}");
    /// Console.WriteLine($"Total pending: {info.TotalPending}");
    /// 
    /// foreach (var consumer in info.ConsumerPending)
    /// {
    ///     Console.WriteLine($"  {consumer.Key}: {consumer.Value} pending");
    /// }
    /// </code>
    /// </example>
    public async Task<RedisStreamInfo> Info(string streamKey, string? group = null)
    {
        try
        {
            var db = _redis.GetDatabase();
            var info = new RedisStreamInfo();

            // Get stream length
            info.Length = await db.StreamLengthAsync(streamKey);

            // Get pending info if group specified
            if (!string.IsNullOrEmpty(group))
            {
                try
                {
                    var pendingInfo = await db.StreamPendingAsync(streamKey, group);
                    info.TotalPending = pendingInfo.PendingMessageCount;

                    // Get per-consumer pending counts
                    foreach (var consumer in pendingInfo.Consumers)
                    {
                        info.ConsumerPending[consumer.Name!] = consumer.PendingMessageCount;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not get pending info for group {group} in stream {streamKey}");
                }
            }

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting info for stream {StreamKey}", streamKey);
            throw;
        }
    }

    /// <summary>
    /// Stops a stream listener that was started with <see cref="Listen"/>.
    /// 
    /// Signals cancellation and waits up to 10 seconds for the background task to complete
    /// gracefully. Any messages being processed will finish before the listener stops.
    /// </summary>
    /// <param name="handle">The listen handle returned by <see cref="Listen"/>.</param>
    /// <example>
    /// <code>
    /// // Start listening
    /// var handle = redisService.Listen("jobs:chat", "executor", "consumer1", handler);
    /// 
    /// // Later, stop the listener gracefully
    /// await redisService.StopListen(handle);
    /// </code>
    /// </example>
    public async Task StopListen(ListenHandle handle)
    {
        if (handle == null)
            return;

        try
        {
            _logger.LogInformation($"Stopping listener for stream {handle.StreamKey}");
            
            // Signal cancellation
            handle.CancellationTokenSource.Cancel();

            // Wait for background task to complete (with timeout)
            if (handle.BackgroundTask != null)
            {
                await Task.WhenAny(handle.BackgroundTask, Task.Delay(10000));
            }

            handle.CancellationTokenSource.Dispose();
            
            _logger.LogInformation($"Stopped listener for stream {handle.StreamKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping listener for stream {StreamKey}", handle.StreamKey);
        }
    }

    /// <summary>
    /// Ensures a consumer group exists for a stream, creating it if necessary.
    /// </summary>
    private async Task EnsureConsumerGroupExists(IDatabase db, string streamKey, string group)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(streamKey, group, StreamPosition.Beginning, createStream: true);
            _logger.LogInformation($"Created consumer group {group} for stream {streamKey}");
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Consumer group already exists, which is fine
            _logger.LogDebug($"Consumer group {group} already exists for stream {streamKey}");
        }
    }

    #endregion

    #region IRedisCacheService - Cache Operations

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
    /// var summary = await redisService.Get("node:summary:abc123");
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
    /// 
    /// <para>
    /// <b>Note:</b> This is different from <see cref="Add"/> which appends to a Redis Stream.
    /// Use <c>Set</c> for caching single values, use <c>Add</c> for message queuing.
    /// </para>
    /// </summary>
    /// <param name="key">The Redis key (e.g., "node:summary:abc123").</param>
    /// <param name="value">The string value to store.</param>
    /// <param name="expiry">Optional expiration time. If null, the key never expires.</param>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Cache a summary with no expiration
    /// await redisService.Set("node:summary:abc123", "This is a conversation about...");
    /// 
    /// // Cache a value that expires in 1 hour
    /// await redisService.Set("temp:session:xyz", "session-data", TimeSpan.FromHours(1));
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
    /// if (await redisService.Exists("node:summary:abc123"))
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
    /// var deleted = await redisService.Remove("node:summary:abc123");
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
    /// var newLength = await redisService.Update(historyKey, messageJson);
    /// Console.WriteLine($"History now has {newLength} messages");
    /// 
    /// // Trim to keep only the last 20 messages
    /// if (newLength > 20)
    /// {
    ///     await redisService.TrimList(historyKey, -20, -1);
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
    /// var allMessages = await redisService.Range("node:history:abc123");
    /// 
    /// // Get only the last 10 messages
    /// var recentMessages = await redisService.Range("node:history:abc123", -10, -1);
    /// 
    /// // Get first 5 messages
    /// var firstMessages = await redisService.Range("node:history:abc123", 0, 4);
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
    /// 
    /// <para>
    /// <b>Note:</b> This is for Redis Lists. For Redis Streams, use <see cref="Trim"/>.
    /// </para>
    /// </summary>
    /// <param name="key">The Redis List key.</param>
    /// <param name="start">Start index to keep (0-based, supports negative).</param>
    /// <param name="stop">Stop index to keep (inclusive, supports negative).</param>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Keep only the last 20 messages in history
    /// await redisService.TrimList("node:history:abc123", -20, -1);
    /// 
    /// // Keep only first 10 messages
    /// await redisService.TrimList("node:history:abc123", 0, 9);
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
    /// var historyLength = await redisService.Length("node:history:abc123");
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

    #endregion
}
