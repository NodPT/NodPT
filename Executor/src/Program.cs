using BackendExecutor;
using BackendExecutor.Config;
using BackendExecutor.Data;
using BackendExecutor.Services;
using NodPT.Data.Services;
using StackExchange.Redis;
using DevExpress.Xpo;
using NodPT.Data.DTOs;
using RedisService.Cache;
using RedisService.Queue;

var builder = Host.CreateApplicationBuilder(args);

// Configure options (Note: Redis connection is now configured separately from ExecutorOptions)
builder.Services.Configure<ExecutorOptions>(options =>
{
    // Read from environment variables with defaults
    options.RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:8847";
    options.MaxManager = int.TryParse(Environment.GetEnvironmentVariable("MAX_MANAGER"), out var maxManager) ? maxManager : 0;
    options.MaxInspector = int.TryParse(Environment.GetEnvironmentVariable("MAX_INSPECTOR"), out var maxInspector) ? maxInspector : 0;
    options.MaxAgent = int.TryParse(Environment.GetEnvironmentVariable("MAX_AGENT"), out var maxAgent) ? maxAgent : 0;
    options.MaxTotal = int.TryParse(Environment.GetEnvironmentVariable("MAX_TOTAL"), out var maxTotal) ? maxTotal : 0;
    options.LlmEndpoint = Environment.GetEnvironmentVariable("LLM_ENDPOINT") ?? "http://localhost:8355/v1/chat/completions";
});

// Register ExecutorOptions as singleton
builder.Services.AddSingleton<ExecutorOptions>(provider =>
{
    var options = new ExecutorOptions();
    provider.GetRequiredService<IConfiguration>().GetSection(ExecutorOptions.SectionName).Bind(options);
    
    // Override with environment variables
    options.RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? options.RedisConnection;
    options.MaxManager = int.TryParse(Environment.GetEnvironmentVariable("MAX_MANAGER"), out var maxManager) ? maxManager : options.MaxManager;
    options.MaxInspector = int.TryParse(Environment.GetEnvironmentVariable("MAX_INSPECTOR"), out var maxInspector) ? maxInspector : options.MaxInspector;
    options.MaxAgent = int.TryParse(Environment.GetEnvironmentVariable("MAX_AGENT"), out var maxAgent) ? maxAgent : options.MaxAgent;
    options.MaxTotal = int.TryParse(Environment.GetEnvironmentVariable("MAX_TOTAL"), out var maxTotal) ? maxTotal : options.MaxTotal;
    options.LlmEndpoint = Environment.GetEnvironmentVariable("LLM_ENDPOINT") ?? options.LlmEndpoint;
    
    return options;
});

// Register SummarizationOptions
builder.Services.AddSingleton<SummarizationOptions>(provider =>
{
    var options = new SummarizationOptions();
    provider.GetRequiredService<IConfiguration>().GetSection("Summarization").Bind(options);
    
    // Override with environment variables
    options.BaseUrl = Environment.GetEnvironmentVariable("SUMMARIZATION_BASE_URL") ?? options.BaseUrl;
    options.Model = Environment.GetEnvironmentVariable("SUMMARIZATION_MODEL") ?? options.Model;
    if (int.TryParse(Environment.GetEnvironmentVariable("SUMMARIZATION_TIMEOUT_SECONDS"), out var timeout))
        options.TimeoutSeconds = timeout;
    if (int.TryParse(Environment.GetEnvironmentVariable("SUMMARIZATION_MAX_LENGTH"), out var maxLen))
        options.MaxSummaryLength = maxLen;
    
    return options;
});

// Register MemoryOptions
builder.Services.AddSingleton<MemoryOptions>(provider =>
{
    var options = new MemoryOptions();
    provider.GetRequiredService<IConfiguration>().GetSection("Memory").Bind(options);
    
    // Override with environment variables
    if (int.TryParse(Environment.GetEnvironmentVariable("MEMORY_HISTORY_LIMIT"), out var historyLimit))
        options.HistoryLimit = historyLimit;
    options.SummaryKeyPrefix = Environment.GetEnvironmentVariable("MEMORY_SUMMARY_KEY_PREFIX") ?? options.SummaryKeyPrefix;
    options.HistoryKeyPrefix = Environment.GetEnvironmentVariable("MEMORY_HISTORY_KEY_PREFIX") ?? options.HistoryKeyPrefix;
    
    return options;
});

// Register Redis
var redisConnection = builder.Configuration["Redis:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION")
    ?? "localhost:8847";

// Add abortConnect=false to allow retry behavior when Redis is unavailable
var redisOptions = ConfigurationOptions.Parse(redisConnection);
redisOptions.AbortOnConnectFail = false;
redisOptions.ConnectTimeout = 5000;
redisOptions.SyncTimeout = 5000;

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var logger = provider.GetService<ILogger<Program>>();
    logger?.LogInformation($"Connecting to Redis at {redisConnection}...");
    var connection = ConnectionMultiplexer.Connect(redisOptions);
    
    if (connection.IsConnected)
    {
        logger?.LogInformation("Successfully connected to Redis");
    }
    else
    {
        logger?.LogWarning($"Redis connection created but not yet connected. Will retry in background.");
    }
    
    return connection;
});

builder.Services.AddSingleton<IDatabase>(provider =>
{
    var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    return multiplexer.GetDatabase();
});

// Register Redis Cache and Queue Services
builder.Services.AddSingleton<RedisCacheService>(provider =>
{
    var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    var logger = provider.GetRequiredService<ILogger<RedisCacheService>>();
    return new RedisCacheService(multiplexer, logger);
});

builder.Services.AddSingleton<RedisQueueService>(provider =>
{
    var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    var logger = provider.GetRequiredService<ILogger<RedisQueueService>>();
    return new RedisQueueService(multiplexer, logger);
});

// Register HttpClient for LLM service
builder.Services.AddHttpClient<LlmChatService, LlmChatService>();

// Register HttpClient for SummarizationService
builder.Services.AddHttpClient<SummarizationService, SummarizationService>((provider, client) =>
{
    var options = provider.GetRequiredService<SummarizationOptions>();
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

// Register MemoryService
builder.Services.AddSingleton<MemoryService>(provider =>
{
    var redisService = provider.GetRequiredService<RedisCacheService>();
    var summarizationService = provider.GetRequiredService<SummarizationService>();
    var options = provider.GetRequiredService<MemoryOptions>();
    var logger = provider.GetRequiredService<ILogger<MemoryService>>();
    var serviceScopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    return new MemoryService(redisService, summarizationService, options, logger);
});

// Register database services
builder.Services.AddScoped<UnitOfWork>(provider => new UnitOfWork());

// NEW: ChatStreamWorker using unified RedisService
builder.Services.AddHostedService<ChatStreamWorker>();

var host = builder.Build();

// Log configuration on startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var executorOptions = host.Services.GetRequiredService<ExecutorOptions>();
var summarizationOptions = host.Services.GetRequiredService<SummarizationOptions>();
var memoryOptions = host.Services.GetRequiredService<MemoryOptions>();

logger.LogInformation("BackendExecutor starting with configuration:");
logger.LogInformation("  Redis Connection: {RedisConnection}", redisConnection);
logger.LogInformation("  LLM Endpoint: {LlmEndpoint}", executorOptions.LlmEndpoint);
logger.LogInformation("  Max Manager: {MaxManager}", executorOptions.MaxManager == 0 ? "unlimited" : executorOptions.MaxManager);
logger.LogInformation("  Max Inspector: {MaxInspector}", executorOptions.MaxInspector == 0 ? "unlimited" : executorOptions.MaxInspector);
logger.LogInformation("  Max Agent: {MaxAgent}", executorOptions.MaxAgent == 0 ? "unlimited" : executorOptions.MaxAgent);
logger.LogInformation("  Max Total: {MaxTotal}", executorOptions.MaxTotal == 0 ? "unlimited" : executorOptions.MaxTotal);
logger.LogInformation("  Summarization Base URL: {BaseUrl}", summarizationOptions.BaseUrl);
logger.LogInformation("  Summarization Model: {Model}", summarizationOptions.Model);
logger.LogInformation("  Memory History Limit: {HistoryLimit}", memoryOptions.HistoryLimit);

host.Run();
