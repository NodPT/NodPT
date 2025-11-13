using BackendExecutor.Data;
using BackendExecutor.Runners;
using BackendExecutor.Config;

namespace BackendExecutor.Dispatch;

/// <summary>
/// Interface for job dispatcher
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Dispatch a job for execution
    /// </summary>
    /// <param name="job">Job envelope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DispatchAsync(JobEnvelope job, CancellationToken cancellationToken = default);
}

/// <summary>
/// Job dispatcher with concurrency control
/// </summary>
public class JobDispatcher : IDispatcher
{
    private readonly ILogger<JobDispatcher> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ExecutorOptions _options;
    
    private readonly SemaphoreSlim? _managerSemaphore;
    private readonly SemaphoreSlim? _inspectorSemaphore;
    private readonly SemaphoreSlim? _agentSemaphore;
    private readonly SemaphoreSlim? _totalSemaphore;

    public JobDispatcher(
        ILogger<JobDispatcher> logger,
        IServiceProvider serviceProvider,
        ExecutorOptions options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options;

        // Initialize semaphores only if limits are set (> 0)
        _managerSemaphore = options.MaxManager > 0 ? new SemaphoreSlim(options.MaxManager, options.MaxManager) : null;
        _inspectorSemaphore = options.MaxInspector > 0 ? new SemaphoreSlim(options.MaxInspector, options.MaxInspector) : null;
        _agentSemaphore = options.MaxAgent > 0 ? new SemaphoreSlim(options.MaxAgent, options.MaxAgent) : null;
        _totalSemaphore = options.MaxTotal > 0 ? new SemaphoreSlim(options.MaxTotal, options.MaxTotal) : null;
    }

    public async Task DispatchAsync(JobEnvelope job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dispatcher: Dispatching job {JobId} with role {Role}", job.JobId, job.Role);

        var roleSemaphore = GetRoleSemaphore(job.Role);
        
        // Acquire semaphores in order: global, then role-specific
        try
        {
            if (_totalSemaphore != null)
                await _totalSemaphore.WaitAsync(cancellationToken);

            if (roleSemaphore != null)
                await roleSemaphore.WaitAsync(cancellationToken);

            // Get the appropriate runner and execute
            var runner = GetRunner(job.Role);
            var result = await runner.RunAsync(job, cancellationToken);

            // Save result and notify
            var repository = _serviceProvider.GetRequiredService<IRepository>();
            var notifier = _serviceProvider.GetRequiredService<Notify.INotifier>();

            await repository.UpsertResultAsync(job.JobId, result.Status, result.Output, cancellationToken);
            await notifier.NotifyAsync(job.ConnectionId, "job_completed", new { jobId = job.JobId, result }, cancellationToken);

            _logger.LogInformation("Dispatcher: Completed job {JobId}", job.JobId);
        }
        finally
        {
            // Release semaphores in reverse order
            roleSemaphore?.Release();
            _totalSemaphore?.Release();
        }
    }

    private SemaphoreSlim? GetRoleSemaphore(string role) => role.ToLowerInvariant() switch
    {
        "manager" => _managerSemaphore,
        "inspector" => _inspectorSemaphore,
        "agent" => _agentSemaphore,
        _ => null
    };

    private IJobRunner GetRunner(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "manager" => _serviceProvider.GetRequiredService<ManagerRunner>(),
            "inspector" => _serviceProvider.GetRequiredService<InspectorRunner>(),
            "agent" => _serviceProvider.GetRequiredService<AgentRunner>(),
            _ => throw new InvalidOperationException($"Unknown role: {role}")
        };
    }
}