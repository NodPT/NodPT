using BackendExecutor.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace BackendExecutor.Consumers;

/// <summary>
/// Interface for Redis consumer
/// </summary>
public interface IRedisConsumer
{
    /// <summary>
    /// Start consuming messages from Redis Streams
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis Streams consumer implementation
/// </summary>
public class RedisConsumer : IRedisConsumer
{
    private readonly ILogger<RedisConsumer> _logger;
    private readonly IDatabase _database;
    private readonly Dispatch.IDispatcher _dispatcher;
    
    private const string ConsumerGroup = "executor-group";
    private const string ConsumerName = "executor-consumer";
    
    private static readonly string[] StreamNames = {
        "jobs:manager",
        "jobs:inspector", 
        "jobs:agent"
    };

    public RedisConsumer(
        ILogger<RedisConsumer> logger,
        IDatabase database,
        Dispatch.IDispatcher dispatcher)
    {
        _logger = logger;
        _database = database;
        _dispatcher = dispatcher;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RedisConsumer: Starting to consume from streams: {Streams}", string.Join(", ", StreamNames));

        // Ensure consumer groups exist
        await EnsureConsumerGroupsExist();

        var streamPositions = StreamNames.Select(stream => new StreamPosition(stream, StreamPosition.NewMessages)).ToArray();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var results = await _database.StreamReadGroupAsync(
                    streamPositions,
                    ConsumerGroup,
                    ConsumerName,
                    countPerStream: 1,
                    noAck: false);

                foreach (var streamResult in results)
                {
                    foreach (var entry in streamResult.Entries)
                    {
                        await ProcessMessage(streamResult.Key!, entry, cancellationToken);
                        
                        // Acknowledge the message
                        await _database.StreamAcknowledgeAsync(streamResult.Key!, ConsumerGroup, entry.Id);
                    }
                }

                // Brief delay if no messages to avoid tight loop
                if (results.All(r => r.Entries.Length == 0))
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RedisConsumer: Stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RedisConsumer: Error while consuming messages");
                await Task.Delay(5000, cancellationToken); // Wait before retrying
            }
        }
    }

    private async Task ProcessMessage(RedisKey streamName, StreamEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("RedisConsumer: Processing message {MessageId} from stream {Stream}", entry.Id, streamName);

            var job = ParseJobEnvelope(streamName!, entry);
            await _dispatcher.DispatchAsync(job, cancellationToken);
            
            _logger.LogInformation("RedisConsumer: Successfully dispatched job {JobId}", job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RedisConsumer: Failed to process message {MessageId} from stream {Stream}", entry.Id, streamName);
        }
    }

    private static JobEnvelope ParseJobEnvelope(string streamName, StreamEntry entry)
    {
        var fields = entry.Values.ToDictionary(kv => kv.Name.ToString(), kv => kv.Value.ToString());
        
        // Extract role from stream name (e.g., "jobs:manager" -> "manager")
        var role = streamName.Split(':').LastOrDefault() ?? "unknown";
        
        var payload = new Dictionary<string, object>();
        if (fields.TryGetValue("payload", out var payloadJson) && !string.IsNullOrEmpty(payloadJson))
        {
            try
            {
                payload = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson) ?? new();
            }
            catch
            {
                // If payload parsing fails, keep empty dictionary
            }
        }

        return new JobEnvelope
        {
            JobId = fields.GetValueOrDefault("jobId", Guid.NewGuid().ToString()),
            WorkflowId = fields.GetValueOrDefault("workflowId", "unknown"),
            Role = role,
            ConnectionId = fields.GetValueOrDefault("connectionId", ""),
            Task = fields.GetValueOrDefault("task", ""),
            Payload = payload
        };
    }

    private async Task EnsureConsumerGroupsExist()
    {
        foreach (var streamName in StreamNames)
        {
            try
            {
                await _database.StreamCreateConsumerGroupAsync(streamName, ConsumerGroup, StreamPosition.Beginning, createStream: true);
                _logger.LogInformation("RedisConsumer: Created consumer group {Group} for stream {Stream}", ConsumerGroup, streamName);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                // Consumer group already exists, which is fine
                _logger.LogDebug("RedisConsumer: Consumer group {Group} already exists for stream {Stream}", ConsumerGroup, streamName);
            }
        }
    }
}