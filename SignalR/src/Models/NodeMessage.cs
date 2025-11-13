namespace NodPT.SignalR.Models;

public class NodeMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ClientConnectionId { get; set; } = string.Empty;
    public string WorkflowGroup { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
