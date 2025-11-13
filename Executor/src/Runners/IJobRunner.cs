using BackendExecutor.Data;

namespace BackendExecutor.Runners;

/// <summary>
/// Interface for job runners
/// </summary>
public interface IJobRunner
{
    /// <summary>
    /// Execute a job
    /// </summary>
    /// <param name="job">Job envelope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job result with status and output</returns>
    Task<JobResult> RunAsync(JobEnvelope job, CancellationToken cancellationToken = default);
}

/// <summary>
/// Job execution result
/// </summary>
public record JobResult
{
    public required string Status { get; init; }
    public object Output { get; init; } = new { };
}