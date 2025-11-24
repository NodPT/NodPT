using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NodPT.Data.Services;
using NodPT.API.Services;
using NodPT.API.Hubs;
using NodPT.API.BackgroundServices;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StackExchange.Redis;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

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
var redisConnection = builder.Configuration["Redis:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION")
    ?? "localhost:6379";

// Configure Redis connection with error handling and logging
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    try
    {
        var logger = provider.GetService<ILogger<Program>>();
        logger?.LogInformation($"Connecting to Redis at {redisConnection}...");
        var connection = ConnectionMultiplexer.Connect(redisConnection);
        logger?.LogInformation("Successfully connected to Redis");
        return connection;
    }
    catch (Exception ex)
    {
        var logger = provider.GetService<ILogger<Program>>();
        logger?.LogError(ex, $"Failed to connect to Redis at {redisConnection}. Please ensure Redis is running and accessible.");
        throw;
    }
});

builder.Services.AddSingleton<IRedisService, NodPT.Data.Services.RedisService>();

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
});

// 🔹 CORS
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
}
catch (Exception ex)
{
    Console.WriteLine($"To enable Firebase authentication, set GOOGLE_APPLICATION_CREDENTIALS environment variable. Error: {ex.Message}");
}

// Add authentication using Firebase JWTs via JWT Bearer
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

builder.Services.AddAuthorization();

// 🔹 Build and run app
var app = builder.Build();

app.UseRouting(); // 🔹 Enable routing
app.UseCors("AllowAll"); // 🔹 Enable CORS
app.UseAuthentication(); // 🔹 Enable authentication
app.UseAuthorization(); // 🔹 Enable authorization

// 🔹 Map the SignalR hub
app.MapHub<NodptHub>("/signalr").RequireAuthorization();

app.MapControllers(); // 🔹 Map controllers

app.Run();



