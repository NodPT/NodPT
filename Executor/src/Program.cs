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
using NodPT.Utils;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
// 🔹 Load environment variables
Common.LoadEnvVariables(builder);

// 🔹 Database initialization
DatabaseInitializer.Initialize(builder);

// Redis configuration
Common.SetupRedis<Program>(builder);

#region  Executor Options and Services Setup

// Configure options (Note: Redis connection is now configured separately from ExecutorOptions)
builder.Services.Configure<ExecutorOptions>(options =>
{
    // Read from environment variables with defaults
    options.RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
    options.MaxManager = int.TryParse(Environment.GetEnvironmentVariable("MAX_MANAGER"), out var maxManager) ? maxManager : 0;
    options.MaxInspector = int.TryParse(Environment.GetEnvironmentVariable("MAX_INSPECTOR"), out var maxInspector) ? maxInspector : 0;
    options.MaxAgent = int.TryParse(Environment.GetEnvironmentVariable("MAX_AGENT"), out var maxAgent) ? maxAgent : 0;
    options.MaxTotal = int.TryParse(Environment.GetEnvironmentVariable("MAX_TOTAL"), out var maxTotal) ? maxTotal : 0;
    options.LlmEndpoint = Environment.GetEnvironmentVariable("LLM_ENDPOINT") ?? "http://ollama:11434/api/generate";
    options.DefaultModel = Environment.GetEnvironmentVariable("DEFAULT_MODEL") ?? "deepseek-r1:1.5b";
    options.LlmTimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("LLM_TIMEOUT_SECONDS"), out var llmTimeout) ? llmTimeout : 300;
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
    options.DefaultModel = Environment.GetEnvironmentVariable("DEFAULT_MODEL") ?? options.DefaultModel;
    options.LlmTimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("LLM_TIMEOUT_SECONDS"), out var llmTimeout) ? llmTimeout : options.LlmTimeoutSeconds;

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
#endregion

// Register HttpClientFactory with timeout configuration for LLM services
builder.Services.AddHttpClient();

// Register LLM services with configured HttpClient and LLM timeout
// Note: AddHttpClient<T> registers the service as transient by default
builder.Services.AddHttpClient<OllamaLlmClient>((provider, client) =>
{
    var executorOptions = provider.GetRequiredService<ExecutorOptions>();
    client.Timeout = TimeSpan.FromSeconds(executorOptions.LlmTimeoutSeconds);
});

builder.Services.AddHttpClient<TensorRtChatClient>((provider, client) =>
{
    var executorOptions = provider.GetRequiredService<ExecutorOptions>();
    client.Timeout = TimeSpan.FromSeconds(executorOptions.LlmTimeoutSeconds);
});

// Register LlmChatService
builder.Services.AddSingleton<LlmChatService>();

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

host.Run();
