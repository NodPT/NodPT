namespace BackendExecutor.Notify;

/// <summary>
/// Interface for SignalR notification operations
/// </summary>
public interface INotifier
{
    /// <summary>
    /// Send notification via SignalR to a specific connection
    /// </summary>
    /// <param name="connectionId">SignalR connection ID</param>
    /// <param name="eventName">Event name</param>
    /// <param name="payload">Event payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyAsync(string connectionId, string eventName, object payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stub implementation of notifier interface
/// </summary>
public class StubNotifier : INotifier
{
    private readonly ILogger<StubNotifier> _logger;

    public StubNotifier(ILogger<StubNotifier> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(string connectionId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Notifier: Sending event {EventName} to connection {ConnectionId}", eventName, connectionId);
        
        // Stub implementation - in real implementation would send via SignalR
        return Task.CompletedTask;
    }
}