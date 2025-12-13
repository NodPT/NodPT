using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using NodPT.Data.Models;
using System.Collections.Concurrent;

namespace RedisService.Queue;

/// <summary>
/// Redis Queue service providing message queue operations using Redis Streams.
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
public class RedisQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisQueueService> _logger;
    private readonly ConcurrentDictionary<string, int> _retryCounters = new();
    
    // Connection timeout configuration
    private const int ConnectionWaitTimeoutMs = 30000;
    private const int ConnectionCheckIntervalMs = 500;
    
    // Retry configuration
    private const int MaxRetries = 5;
    private const int InitialRetryDelayMs = 1000;

    /// <summary>
    /// Initializes a new instance of the RedisQueueService.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when redis or logger is null.</exception>
    public RedisQueueService(IConnectionMultiplexer redis, ILogger<RedisQueueService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds a message to a Redis Stream for asynchronous processing by consumers.
    /// 
    /// Redis Streams are append-only logs ideal for message queuing between services.
    /// Each message gets a unique entry ID (timestamp-based) and can be consumed by 
    /// multiple consumer groups.
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
    /// var entryId = await queueService.Add("jobs:chat", envelope);
    /// // entryId = "1609459200000-0"
    /// </code>
    /// </example>
    public async Task<string> Add(string streamKey, IDictionary<string, string> envelope)
    {
        try
        {
            // Check if Redis is connected
            if (!_redis.IsConnected)
            {
                _logger.LogWarning($"Redis not connected when trying to add message to stream {streamKey}");
                throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis connection is not available");
            }

            var db = _redis.GetDatabase();
            
            // Convert dictionary to NameValueEntry array
            var entries = envelope.Select(kv => new NameValueEntry(kv.Key, kv.Value)).ToArray();
            
            // Add to stream
            var entryId = await db.StreamAddAsync(streamKey, entries);
            
            _logger.LogDebug($"Added message to Redis stream {streamKey}: {entryId}");
            
            return entryId.ToString();
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection error when adding message to stream: {StreamKey}", streamKey);
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout when adding message to Redis stream: {StreamKey}", streamKey);
            throw;
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
    /// var handle = queueService.Listen(
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
    /// await queueService.StopListen(handle);
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
            var cancellationToken = handle.CancellationTokenSource.Token;

            try
            {
                // Wait for Redis connection to be established
                _logger.LogInformation($"Waiting for Redis connection before starting listener for stream {streamKey}...");
                var connected = await WaitForRedisConnection(ConnectionWaitTimeoutMs);
                
                if (!connected)
                {
                    _logger.LogError($"Failed to establish Redis connection for stream {streamKey}. Listener will not start.");
                    return;
                }

                var db = _redis.GetDatabase();

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
    /// await queueService.Acknowledge("jobs:chat", "executor", "1609459200000-0");
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
    /// var claimedCount = await queueService.ClaimPending(
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
    /// </summary>
    /// <param name="streamKey">The Redis Stream key to trim.</param>
    /// <param name="maxLen">The approximate maximum number of entries to keep.</param>
    /// <exception cref="RedisException">Thrown when Redis operation fails.</exception>
    /// <example>
    /// <code>
    /// // Keep approximately the last 10,000 messages in the stream
    /// await queueService.Trim("jobs:chat", maxLen: 10000);
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
    /// var info = await queueService.Info("jobs:chat", group: "executor");
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
    /// var handle = queueService.Listen("jobs:chat", "executor", "consumer1", handler);
    /// 
    /// // Later, stop the listener gracefully
    /// await queueService.StopListen(handle);
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
    /// Waits for Redis connection to be established with timeout.
    /// </summary>
    /// <param name="timeoutMs">Maximum time to wait for connection in milliseconds.</param>
    /// <returns>True if connected, false if timeout reached.</returns>
    private async Task<bool> WaitForRedisConnection(int timeoutMs = ConnectionWaitTimeoutMs)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var lastLogTime = 0L;
        const int logIntervalMs = 5000; // Log every 5 seconds to reduce log noise
        
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            if (_redis.IsConnected)
            {
                _logger.LogInformation("Redis connection established");
                return true;
            }
            
            // Log progress every 5 seconds instead of every 500ms
            if (stopwatch.ElapsedMilliseconds - lastLogTime >= logIntervalMs)
            {
                _logger.LogDebug($"Waiting for Redis connection... ({stopwatch.ElapsedMilliseconds}ms elapsed)");
                lastLogTime = stopwatch.ElapsedMilliseconds;
            }
            
            await Task.Delay(ConnectionCheckIntervalMs);
        }
        
        _logger.LogWarning($"Redis connection not established after {timeoutMs}ms");
        return false;
    }

    /// <summary>
    /// Calculates exponential backoff delay in milliseconds.
    /// Uses bit shifting for efficiency: 1s, 2s, 4s, 8s, 16s...
    /// </summary>
    /// <param name="attempt">The attempt number (0-based).</param>
    /// <returns>Delay in milliseconds.</returns>
    private int CalculateExponentialBackoffDelay(int attempt)
    {
        return InitialRetryDelayMs << attempt; // Equivalent to InitialRetryDelayMs * 2^attempt
    }

    /// <summary>
    /// Ensures a consumer group exists for a stream, creating it if necessary.
    /// Implements retry logic with exponential backoff to handle connection issues.
    /// </summary>
    private async Task EnsureConsumerGroupExists(IDatabase db, string streamKey, string group)
    {
        
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                // First, check if Redis is connected
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning($"Redis not connected, waiting before retry (attempt {attempt + 1}/{MaxRetries})");
                    await Task.Delay(CalculateExponentialBackoffDelay(attempt));
                    continue;
                }

                // Attempt to create the consumer group
                await db.StreamCreateConsumerGroupAsync(streamKey, group, StreamPosition.Beginning, createStream: true);
                _logger.LogInformation($"Created consumer group {group} for stream {streamKey}");
                return; // Success
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                // Consumer group already exists, which is fine
                _logger.LogDebug($"Consumer group {group} already exists for stream {streamKey}");
                return; // Success - group exists
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, $"Redis connection error while creating consumer group {group} for stream {streamKey} (attempt {attempt + 1}/{MaxRetries})");
                
                if (attempt < MaxRetries - 1)
                {
                    int delayMs = CalculateExponentialBackoffDelay(attempt);
                    _logger.LogInformation($"Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
                else
                {
                    _logger.LogError(ex, $"Failed to create consumer group {group} for stream {streamKey} after {MaxRetries} attempts. Redis may not be available.");
                    throw; // Re-throw after all retries exhausted
                }
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, $"Timeout while creating consumer group {group} for stream {streamKey} (attempt {attempt + 1}/{MaxRetries})");
                
                if (attempt < MaxRetries - 1)
                {
                    int delayMs = CalculateExponentialBackoffDelay(attempt);
                    _logger.LogInformation($"Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
                else
                {
                    _logger.LogError(ex, $"Failed to create consumer group {group} for stream {streamKey} after {MaxRetries} attempts due to timeout.");
                    throw;
                }
            }
        }
    }
}
