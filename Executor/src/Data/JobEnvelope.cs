namespace BackendExecutor.Data;

/// <summary>
/// Data structure for job information read from Redis Streams
/// </summary>
public record JobEnvelope
{
    public required string JobId { get; init; }
    public required string WorkflowId { get; init; }
    public required string Role { get; init; }
    public required string ConnectionId { get; init; }
    public required string Task { get; init; }
    public Dictionary<string, object> Payload { get; init; } = new();
}