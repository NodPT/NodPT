using BackendExecutor.Consumers;

namespace BackendExecutor;

/// <summary>
/// Background worker for chat job processing using Redis Streams
/// </summary>
public class ChatStreamWorker : BackgroundService
{
    private readonly ILogger<ChatStreamWorker> _logger;
    private readonly IChatStreamConsumer _chatStreamConsumer;

    public ChatStreamWorker(ILogger<ChatStreamWorker> logger, IChatStreamConsumer chatStreamConsumer)
    {
        _logger = logger;
        _chatStreamConsumer = chatStreamConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChatStreamWorker starting at: {time}", DateTimeOffset.Now);
        
        try
        {
            await _chatStreamConsumer.StartAsync(stoppingToken);
            
            // Keep the worker running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ChatStreamWorker stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatStreamWorker encountered an error");
            throw;
        }
        finally
        {
            _logger.LogInformation("ChatStreamWorker stopped at: {time}", DateTimeOffset.Now);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ChatStreamWorker stopping...");
        
        await _chatStreamConsumer.StopAsync();
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("ChatStreamWorker stopped");
    }
}
