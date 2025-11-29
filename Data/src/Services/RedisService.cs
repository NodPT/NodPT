using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using NodPT.Data.Models;
using System.Collections.Concurrent;
using NodPT.Data.Interfaces;

namespace NodPT.Data.Services;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisService> _logger;
    private readonly ConcurrentDictionary<string, int> _retryCounters = new();

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
                        info.ConsumerPending[consumer.Name] = consumer.PendingMessageCount;
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

    // ============================================================
    // Key-Value Operations for Memory (Summary and History)
    // ============================================================

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
