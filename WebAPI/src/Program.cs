using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NodPT.API.BackgroundServices;
using NodPT.API.Hubs;
using NodPT.Data.Services;
using RedisService.Cache;
using RedisService.Queue;
using StackExchange.Redis;
using System;
using System.Linq;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args); // 🔹 Create builder

// If credentials are not available, log a warning but continue
// This allows the server to run in development mode without Firebase

// 🔹 Load environment variables
#if DEBUG // 🔹 Load .env in development
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
// In production, load environment variables from system environment
builder.Configuration.AddEnvironmentVariables();
#endif



// 🔹 Database initialization
DatabaseInitializer.Initialize(builder);

// 🔹 Redis
#region Redis Configuration
var redisConnection = builder.Configuration["Redis:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION")
    ?? "localhost:6379";

// Add abortConnect=false to allow retry behavior when Redis is unavailable
var redisOptions = ConfigurationOptions.Parse(redisConnection);
redisOptions.AbortOnConnectFail = false;
redisOptions.ConnectTimeout = 5000;
redisOptions.SyncTimeout = 5000;

// Configure Redis connection with error handling and logging
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var logger = provider.GetService<ILogger<Program>>();

    try
    {
        logger?.LogInformation("Connecting to Redis at {RedisConnection}...", redisConnection);
        var connection = ConnectionMultiplexer.Connect(redisOptions);

        // ConnectionMultiplexer will handle reconnects in the background (AbortOnConnectFail=false)
        // No need to force a ping here; rely on built-in retry behavior.

        return connection;
    }
    catch (Exception ex)
    {
        logger?.LogWarning(ex, "Failed to connect to Redis at {RedisConnection}. Redis features will be unavailable. Ensure Redis is running and accessible.", redisConnection);
        // Return connection anyway - it will retry in background with AbortOnConnectFail=false
        return ConnectionMultiplexer.Connect(redisOptions);
    }
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

#endregion


// 🔹 Log Services
builder.Services.AddScoped<LogService>();

// 🔹 Add SignalR services
builder.Services.AddSignalR();

// 🔹 Add SignalR update listener for chat responses (NEW: uses Redis Streams)
builder.Services.AddHostedService<SignalRUpdateListener>();

// 🔹 Controllers and JSON options of XPO ORM
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    // Preserve original C# property names (PascalCase) in JSON output instead of converting to camelCase
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    // Serialize enums as strings instead of integers
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// 🔹 CORS
#region CORS Setup
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
Console.WriteLine($"Configuring CORS:. allowed origins: {string.Join(", ", allowedOrigins ?? Array.Empty<string>())}");
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // get the allowed origins from configuration appsettings.json
        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR
        }
    });
});
#endregion

// 🔹 Firebase Authentication setup
#region Firebase Helper Class

string? firebaseProjectId = builder.Configuration["Firebase:ProjectId"]
 ?? Environment.GetEnvironmentVariable("VITE_FIREBASE_PROJECT_ID")
 ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
 ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");

if (string.IsNullOrWhiteSpace(firebaseProjectId))
{
    Console.WriteLine("ERROR: Firebase project id not configured. Set Firebase:ProjectId or env variable VITE_FIREBASE_PROJECT_ID.");
    throw new InvalidOperationException("Firebase project id not configured");
}

//! Initialize Firebase Admin SDK
// Note: For production, you should set GOOGLE_APPLICATION_CREDENTIALS environment variable
try
{
    if (FirebaseApp.DefaultInstance == null)
    {
        var credentialJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (!string.IsNullOrWhiteSpace(credentialJson))
        {
            try
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(credentialJson)
                });
                Console.WriteLine("Firebase Admin SDK initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize FirebaseApp: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("WARNING: GOOGLE_APPLICATION_CREDENTIALS env var not set (expects JSON content).");
        }
    }
    else
    {
        Console.WriteLine("Firebase Admin SDK already initialized. Skipping initialization.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error checking Firebase initialization status: {ex.Message}");
}



//! Add authentication using Firebase JWTs via JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        string JwksUrl = $"https://securetoken.google.com/{firebaseProjectId}";
        options.Authority = JwksUrl; // 🔹 Set the authority to Firebase JWKS URL
        options.Audience = firebaseProjectId; // Audience must match project id
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // 🔹 Validate the issuer of the token
            ValidIssuer = JwksUrl,
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true,
            // Provide signing keys from Google's JWKS (Firebase)
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                var keys = FirebaseHelper.FirebaseKeysProvider.GetSigningKeys(); // Get signing keys from Firebase 
                if (!string.IsNullOrEmpty(kid))
                {
                    // Match the key id (kid) with the keys from Firebase
                    var matched = keys.Where(k => (k.KeyId?.Equals(kid, StringComparison.Ordinal)) == true).ToList<SecurityKey>();
                    if (matched.Count > 0)
                        return matched; // Return matched key(s)
                }
                return keys.ToList<SecurityKey>(); // Fallback to all keys if no key match
            }
        };

        // Include error details in development
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();

        // Configure for SignalR to use query string token
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/signalr"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

#endregion

// 🔹 Add authorization services
builder.Services.AddAuthorization();

// 🔹 Build and run app
var app = builder.Build();

// 🔹 Configure DatabaseHelper to use IHttpContextAccessor for request-scoped UnitOfWork
DatabaseHelper.SetHttpContextAccessor(app.Services.GetRequiredService<IHttpContextAccessor>());

app.UseRouting(); // 🔹 Enable routing
app.UseCors("AllowAll"); // 🔹 Enable CORS
app.UseAuthentication(); // 🔹 Enable authentication
app.UseAuthorization(); // 🔹 Enable authorization

// 🔹 Map the SignalR hub
app.MapHub<NodptHub>("/signalr").RequireAuthorization();

app.MapControllers(); // 🔹 Map controllers

app.Run();





