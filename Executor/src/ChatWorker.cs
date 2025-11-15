using BackendExecutor.Consumers;

namespace BackendExecutor;

public class ChatWorker : BackgroundService
{
    private readonly ILogger<ChatWorker> _logger;
    private readonly IChatJobConsumer _chatJobConsumer;

    public ChatWorker(ILogger<ChatWorker> logger, IChatJobConsumer chatJobConsumer)
    {
        _logger = logger;
        _chatJobConsumer = chatJobConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChatWorker starting at: {time}", DateTimeOffset.Now);
        
        try
        {
            await _chatJobConsumer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ChatWorker stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatWorker encountered an error");
            throw;
        }
        finally
        {
            _logger.LogInformation("ChatWorker stopped at: {time}", DateTimeOffset.Now);
        }
    }
}
