namespace NodPT.Data.Models;

/// <summary>
/// Represents a message envelope in Redis Streams
/// </summary>
public class MessageEnvelope
{
    public string StreamKey { get; set; } = string.Empty;
    public string EntryId { get; set; } = string.Empty;
    public Dictionary<string, string> Fields { get; set; } = new();
}

/// <summary>
/// Options for configuring the Listen method
/// </summary>
public class ListenOptions
{
    /// <summary>
    /// Number of messages to read per batch (default: 10)
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Number of concurrent message handlers (default: 1)
    /// </summary>
    public int Concurrency { get; set; } = 1;

    /// <summary>
    /// Idle threshold in milliseconds for claiming pending messages (default: 60000 = 1 minute)
    /// </summary>
    public int ClaimIdleThresholdMs { get; set; } = 60000;

    /// <summary>
    /// Maximum retry attempts before moving to dead letter (default: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Polling delay in milliseconds when no messages (default: 1000 = 1 second)
    /// </summary>
    public int PollDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to create the stream and consumer group if missing (default: true)
    /// </summary>
    public bool CreateStreamIfMissing { get; set; } = true;

    /// <summary>
    /// Whether to claim pending messages on startup (default: true)
    /// </summary>
    public bool ClaimPendingOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to delete (XDEL) after acknowledge (default: false, only XACK)
    /// </summary>
    public bool DeleteAfterAck { get; set; } = false;
}

/// <summary>
/// Handle for a Listen operation that can be used to stop it
/// </summary>
public class ListenHandle
{
    internal CancellationTokenSource CancellationTokenSource { get; set; } = new();
    internal Task? BackgroundTask { get; set; }
    
    public string StreamKey { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string ConsumerName { get; set; } = string.Empty;
}

/// <summary>
/// Information about a Redis Stream
/// </summary>
public class RedisStreamInfo
{
    /// <summary>
    /// Length of the stream (XLEN)
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// Total pending messages across all consumers
    /// </summary>
    public long TotalPending { get; set; }

    /// <summary>
    /// Per-consumer pending message counts
    /// </summary>
    public Dictionary<string, long> ConsumerPending { get; set; } = new();
}
