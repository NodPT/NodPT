using BackendExecutor;
using BackendExecutor.Config;
using BackendExecutor.Consumers;
using BackendExecutor.Data;
using BackendExecutor.Dispatch;
using BackendExecutor.Notify;
using BackendExecutor.Runners;
using BackendExecutor.Services;
using NodPT.Data.Services;
using StackExchange.Redis;
using DevExpress.Xpo;

var builder = Host.CreateApplicationBuilder(args);

// Configure options
builder.Services.Configure<ExecutorOptions>(options =>
{
    // Read from environment variables with defaults
    options.RedisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
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

// Register Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var options = provider.GetRequiredService<ExecutorOptions>();
    return ConnectionMultiplexer.Connect(options.RedisConnection);
});

builder.Services.AddSingleton<IDatabase>(provider =>
{
    var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    return multiplexer.GetDatabase();
});

// Register RedisService from Data project
builder.Services.AddSingleton<IRedisService>(provider =>
{
    var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    var logger = provider.GetRequiredService<ILogger<RedisService>>();
    return new RedisService(multiplexer, logger);
});

// Register HttpClient for LLM service
builder.Services.AddHttpClient<ILlmChatService, LlmChatService>();

// Register database services
builder.Services.AddScoped<UnitOfWork>(provider => new UnitOfWork());

// Register services
builder.Services.AddSingleton<IRepository, StubRepository>();
builder.Services.AddSingleton<INotifier, StubNotifier>();
builder.Services.AddSingleton<IDispatcher, JobDispatcher>();

// OLD: Direct Redis consumer (obsolete - should use shared RedisService)
// builder.Services.AddSingleton<IRedisConsumer, RedisConsumer>();

// OLD: list-based chat consumer (obsolete - replaced by ChatStreamConsumer)
// builder.Services.AddSingleton<IChatJobConsumer, ChatJobConsumer>();

// Register new Streams-based chat consumer
builder.Services.AddSingleton<IChatStreamConsumer, ChatStreamConsumer>();

// Register runners  
builder.Services.AddSingleton<ManagerRunner>();
builder.Services.AddSingleton<InspectorRunner>();
builder.Services.AddSingleton<AgentRunner>();

// Register workers
// OLD: Worker using direct Redis calls (obsolete - should use shared RedisService)
// builder.Services.AddHostedService<Worker>();

// OLD: ChatWorker using list-based consumer (obsolete - replaced by ChatStreamWorker)
// builder.Services.AddHostedService<ChatWorker>();

// NEW: ChatStreamWorker using unified RedisService
builder.Services.AddHostedService<ChatStreamWorker>();

var host = builder.Build();

// Log configuration on startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var executorOptions = host.Services.GetRequiredService<ExecutorOptions>();

logger.LogInformation("BackendExecutor starting with configuration:");
logger.LogInformation("  Redis Connection: {RedisConnection}", executorOptions.RedisConnection);
logger.LogInformation("  LLM Endpoint: {LlmEndpoint}", executorOptions.LlmEndpoint);
logger.LogInformation("  Max Manager: {MaxManager}", executorOptions.MaxManager == 0 ? "unlimited" : executorOptions.MaxManager);
logger.LogInformation("  Max Inspector: {MaxInspector}", executorOptions.MaxInspector == 0 ? "unlimited" : executorOptions.MaxInspector);
logger.LogInformation("  Max Agent: {MaxAgent}", executorOptions.MaxAgent == 0 ? "unlimited" : executorOptions.MaxAgent);
logger.LogInformation("  Max Total: {MaxTotal}", executorOptions.MaxTotal == 0 ? "unlimited" : executorOptions.MaxTotal);

host.Run();
