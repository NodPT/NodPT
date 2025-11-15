using NodPT.SignalR.Hubs;
using NodPT.SignalR.Authentication;
using NodPT.SignalR.Services;
using Microsoft.AspNetCore.Authentication;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Load .env in development
#if DEBUG
var dotenvPath = Path.Combine(AppContext.BaseDirectory, ".env");
if (File.Exists(dotenvPath))
{
    Console.WriteLine($"Loading .env from {dotenvPath}");
    foreach (var line in File.ReadAllLines(dotenvPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            continue;
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var value = parts[1].Trim().Trim('"');
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
#else
builder.Configuration.AddEnvironmentVariables();
#endif

// 🔹 Firebase Authentication setup
string? firebaseProjectId = builder.Configuration["Firebase:ProjectId"]
 ?? Environment.GetEnvironmentVariable("VITE_FIREBASE_PROJECT_ID")
 ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
 ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");

if (string.IsNullOrWhiteSpace(firebaseProjectId))
{
    Console.WriteLine("ERROR: Firebase project id not configured. Set Firebase:ProjectId or env variable VITE_FIREBASE_PROJECT_ID.");
    throw new InvalidOperationException("Firebase project id not configured");
}

// Initialize Firebase Admin SDK
// Note: For production, you should set GOOGLE_APPLICATION_CREDENTIALS environment variable
// or use builder.Configuration to load credentials from appsettings.json
try
{
    if (FirebaseApp.DefaultInstance == null)
    {
        // Try to get credentials from environment
        var credential = GoogleCredential.GetApplicationDefault();
        FirebaseApp.Create(new AppOptions()
        {
            Credential = credential,
        });
    }
}
catch (Exception ex)
{
    // If credentials are not available, log a warning but continue
    // This allows the server to run in development mode without Firebase
    var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
    logger.LogWarning("Firebase credentials not found. Running in development mode without Firebase authentication validation.");
    logger.LogWarning($"To enable Firebase authentication, set GOOGLE_APPLICATION_CREDENTIALS environment variable. Error: {ex.Message}");

#if DEBUG
    // Create Firebase app without credentials for development
    if (FirebaseApp.DefaultInstance == null)
    {
        try
        {
            FirebaseApp.Create();
        }
        catch
        {
            // Even this can fail, which is fine for development
        }
    }
#endif
}

// Add authentication
builder.Services.AddAuthentication("Firebase")
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", null);

builder.Services.AddAuthorization();

// Configure Redis connection
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
try
{
    var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
    logger.LogInformation($"Connecting to Redis at {redisConnectionString}...");

    var redis = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

    logger.LogInformation("Successfully connected to Redis");
}
catch (Exception ex)
{
    var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
    logger.LogError(ex, $"Failed to connect to Redis at {redisConnectionString}. Please ensure Redis is running and accessible.");
    throw new InvalidOperationException(
        $"Redis connection failed. Ensure Redis is running at {redisConnectionString}. " +
        $"Error: {ex.Message}", ex);
}

// Add SignalR services
builder.Services.AddSignalR();

// Add Redis stream listener background service
builder.Services.AddHostedService<RedisStreamListener>();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // In production, configure specific origins in appsettings.json
        // For now, allowing all origins for development
        // TODO: Replace with specific origins for production deployment
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // In production, restrict to specific origins
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();

            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                throw new InvalidOperationException(
                    "CORS allowed origins must be configured in production. " +
                    "Add 'Cors:AllowedOrigins' section to appsettings.json");
            }
        }
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Serve static files (for monitor page)
app.UseStaticFiles();

// Map the nodpt_hub
app.MapHub<NodptHub>("/nodpt_hub");

app.MapGet("/", () => "NodPT.SignalR is running. Connect to /nodpt_hub or visit /monitor.html for the monitoring page");

app.Run();
