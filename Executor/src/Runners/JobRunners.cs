using BackendExecutor.Data;

namespace BackendExecutor.Runners;

/// <summary>
/// Manager role job runner
/// </summary>
public class ManagerRunner : IJobRunner
{
    private readonly ILogger<ManagerRunner> _logger;

    public ManagerRunner(ILogger<ManagerRunner> logger)
    {
        _logger = logger;
    }

    public async Task<JobResult> RunAsync(JobEnvelope job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manager: Starting job {JobId} for workflow {WorkflowId}", job.JobId, job.WorkflowId);

        // Simulate manager work
        await Task.Delay(100, cancellationToken);

        var result = new JobResult
        {
            Status = "completed",
            Output = new { 
                role = "manager", 
                task = job.Task, 
                result = "Manager job completed successfully",
                processedAt = DateTime.UtcNow
            }
        };

        _logger.LogInformation("Manager: Completed job {JobId}", job.JobId);
        return result;
    }
}

/// <summary>
/// Inspector role job runner
/// </summary>
public class InspectorRunner : IJobRunner
{
    private readonly ILogger<InspectorRunner> _logger;

    public InspectorRunner(ILogger<InspectorRunner> logger)
    {
        _logger = logger;
    }

    public async Task<JobResult> RunAsync(JobEnvelope job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Inspector: Starting job {JobId} for workflow {WorkflowId}", job.JobId, job.WorkflowId);

        // Simulate inspector work
        await Task.Delay(150, cancellationToken);

        var result = new JobResult
        {
            Status = "completed",
            Output = new { 
                role = "inspector", 
                task = job.Task, 
                result = "Inspector job completed successfully",
                inspectedAt = DateTime.UtcNow
            }
        };

        _logger.LogInformation("Inspector: Completed job {JobId}", job.JobId);
        return result;
    }
}

/// <summary>
/// Agent role job runner
/// </summary>
public class AgentRunner : IJobRunner
{
    private readonly ILogger<AgentRunner> _logger;

    public AgentRunner(ILogger<AgentRunner> logger)
    {
        _logger = logger;
    }

    public async Task<JobResult> RunAsync(JobEnvelope job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Agent: Starting job {JobId} for workflow {WorkflowId}", job.JobId, job.WorkflowId);

        // Simulate agent work
        await Task.Delay(200, cancellationToken);

        var result = new JobResult
        {
            Status = "completed",
            Output = new { 
                role = "agent", 
                task = job.Task, 
                result = "Agent job completed successfully",
                executedAt = DateTime.UtcNow
            }
        };

        _logger.LogInformation("Agent: Completed job {JobId}", job.JobId);
        return result;
    }
}