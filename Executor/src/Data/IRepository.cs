namespace BackendExecutor.Data;

/// <summary>
/// Interface for database repository operations
/// </summary>
public interface IRepository
{
    /// <summary>
    /// Save or update job result in the database
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="status">Job completion status</param>
    /// <param name="output">Job output data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpsertResultAsync(string jobId, string status, object output, CancellationToken cancellationToken = default);
}

/// <summary>
/// Stub implementation of repository interface
/// </summary>
public class StubRepository : IRepository
{
    private readonly ILogger<StubRepository> _logger;

    public StubRepository(ILogger<StubRepository> logger)
    {
        _logger = logger;
    }

    public Task UpsertResultAsync(string jobId, string status, object output, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Repository: Saving result for job {JobId} with status {Status}", jobId, status);
        
        // Stub implementation - in real implementation would save to database
        return Task.CompletedTask;
    }
}