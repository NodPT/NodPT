using BackendExecutor;
using BackendExecutor.Config;
using BackendExecutor.Consumers;
using BackendExecutor.Data;
using BackendExecutor.Dispatch;
using BackendExecutor.Notify;
using BackendExecutor.Runners;
using StackExchange.Redis;

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

// Register services
builder.Services.AddSingleton<IRepository, StubRepository>();
builder.Services.AddSingleton<INotifier, StubNotifier>();
builder.Services.AddSingleton<IDispatcher, JobDispatcher>();
builder.Services.AddSingleton<IRedisConsumer, RedisConsumer>();

// Register runners  
builder.Services.AddSingleton<ManagerRunner>();
builder.Services.AddSingleton<InspectorRunner>();
builder.Services.AddSingleton<AgentRunner>();

// Register worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Log configuration on startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var executorOptions = host.Services.GetRequiredService<ExecutorOptions>();

logger.LogInformation("BackendExecutor starting with configuration:");
logger.LogInformation("  Redis Connection: {RedisConnection}", executorOptions.RedisConnection);
logger.LogInformation("  Max Manager: {MaxManager}", executorOptions.MaxManager == 0 ? "unlimited" : executorOptions.MaxManager);
logger.LogInformation("  Max Inspector: {MaxInspector}", executorOptions.MaxInspector == 0 ? "unlimited" : executorOptions.MaxInspector);
logger.LogInformation("  Max Agent: {MaxAgent}", executorOptions.MaxAgent == 0 ? "unlimited" : executorOptions.MaxAgent);
logger.LogInformation("  Max Total: {MaxTotal}", executorOptions.MaxTotal == 0 ? "unlimited" : executorOptions.MaxTotal);

host.Run();
