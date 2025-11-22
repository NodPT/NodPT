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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args); // 🔹 Create builder

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

builder.Services.AddSingleton<IRedisService, RedisService>();

// 🔹 Log Services
builder.Services.AddScoped<LogService>();

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
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
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

// Add authentication using Firebase JWTs
builder.Services.AddAuthorization();
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
 });


// 🔹 Build and run app
var app = builder.Build();

app.UseRouting(); // 🔹 Enable routing
app.UseCors("AllowAll"); // 🔹 Enable CORS
app.UseAuthentication(); // 🔹 Enable authentication
app.UseAuthorization(); // 🔹 Enable authorization
app.MapControllers(); // 🔹 Map controllers

app.Run();



