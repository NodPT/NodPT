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

// 🔹 Database initialization
DatabaseInitializer.Initialize(builder);

// 🔹 Redis
var redisConnection = builder.Configuration["Redis:ConnectionString"] 
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION") 
    ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    return ConnectionMultiplexer.Connect(redisConnection);
});

builder.Services.AddSingleton<IRedisService, RedisService>();

// 🔹 Services
builder.Services.AddScoped<LogService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    // Preserve original C# property names (PascalCase) in JSON output instead of converting to camelCase
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();

// 🔹 CORS
#if DEBUG
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
#else
Console.WriteLine("Configuring CORS for production.");
builder.Services.AddCors(options =>
{
 options.AddPolicy("AllowAll", policy =>
 {
 policy.WithOrigins("https://api.nodpt.com", "https://nodpt.com", "https://*.nodpt.com", "https://www.nodpt.com").AllowAnyHeader().AllowAnyMethod();
 });
});
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
 .AddJwtBearer(options =>
 {
     options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
     options.Audience = firebaseProjectId; // Audience must match project id
     options.TokenValidationParameters = new TokenValidationParameters
     {
         ValidateIssuer = true,
         ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
         ValidateAudience = true,
         ValidAudience = firebaseProjectId,
         ValidateLifetime = true,
         // Provide signing keys from Google's JWKS (Firebase)
         IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
         {
             var keys = FirebaseKeysProvider.GetSigningKeys();
             if (!string.IsNullOrEmpty(kid))
             {
                 var matched = keys.Where(k => (k.KeyId?.Equals(kid, StringComparison.Ordinal)) == true).ToList<SecurityKey>();
                 if (matched.Count > 0) return matched;
             }
             return keys.ToList<SecurityKey>();
         }
     };
     options.IncludeErrorDetails = builder.Environment.IsDevelopment();

     // Diagnostic events
     options.Events = new JwtBearerEvents
     {
         OnMessageReceived = context =>
         {
             if (!context.Request.Headers.ContainsKey("Authorization"))
                 Console.WriteLine("JwtBearer: Authorization header missing.");
             return Task.CompletedTask;
         },
         OnAuthenticationFailed = context =>
         {
             Console.WriteLine($"JwtBearer: Authentication failed: {context.Exception.Message}");
             return Task.CompletedTask;
         },
         OnTokenValidated = context =>
         {
             Console.WriteLine("JwtBearer: Token validated. Claims: " + string.Join(", ", context.Principal?.Claims.Select(c => c.Type + "=" + c.Value) ?? Array.Empty<string>()));
             return Task.CompletedTask;
         },
         OnChallenge = context =>
         {
             Console.WriteLine($"JwtBearer: Challenge issued. Error={context.Error} Desc={context.ErrorDescription}");
             return Task.CompletedTask;
         }
     };
 });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Helper to obtain Firebase signing keys with simple cache
internal static class FirebaseKeysProvider
{
    private static DateTime _lastFetch = DateTime.MinValue;
    private static List<SecurityKey> _keys = new();
    private static readonly object _lock = new();
    private const string JwksUrl = "https://www.googleapis.com/service_accounts/v1/jwk/securetoken@system.gserviceaccount.com";

    public static IEnumerable<SecurityKey> GetSigningKeys()
    {
        lock (_lock)
        {
            if (_keys.Count > 0 && (DateTime.UtcNow - _lastFetch) < TimeSpan.FromHours(12))
            {
                return _keys;
            }

            try
            {
                using var http = new HttpClient();
                var json = http.GetStringAsync(JwksUrl).GetAwaiter().GetResult();
                var jwks = new JsonWebKeySet(json);
                _keys = jwks.Keys.Cast<SecurityKey>().ToList();
                _lastFetch = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch Firebase JWKS: {ex.Message}");
            }

            return _keys;
        }
    }
}

