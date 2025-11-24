using BackendExecutor.Consumers;

namespace BackendExecutor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRedisConsumer _redisConsumer;

    public Worker(ILogger<Worker> logger, IRedisConsumer redisConsumer)
    {
        _logger = logger;
        _redisConsumer = redisConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackendExecutor Worker starting at: {time}", DateTimeOffset.Now);
        
        try
        {
            await _redisConsumer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BackendExecutor Worker stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BackendExecutor Worker encountered an error");
            throw;
        }
        finally
        {
            _logger.LogInformation("BackendExecutor Worker stopped at: {time}", DateTimeOffset.Now);
        }
    }
}
