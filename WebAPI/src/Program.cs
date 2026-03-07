using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NodPT.API.BackgroundServices;
using NodPT.API.Hubs;
using NodPT.API.Services;
using NodPT.Data.Services;
using RedisService.Cache;
using RedisService.Queue;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using NodPT.Utils;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args); // 🔹 Create builder
// 🔹 Load environment variables
Common.LoadEnvVariables(builder);

// 🔹 Database initialization
DatabaseInitializer.Initialize(builder);

// Redis configuration
Common.SetupRedis<Program>(builder);

// 🔹 Register TokenService (JWT generation + Redis-backed token management)
builder.Services.AddScoped<TokenService>();

// 🔹 Add IHttpContextAccessor for HTTP context access
builder.Services.AddHttpContextAccessor();

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



//! Add authentication using our own HMAC-SHA256-signed JWTs
// Firebase tokens are only used inside AuthController.Login for initial identity
// verification (via Firebase Admin SDK). All subsequent API calls use our own
// shorter-lived JWTs so we can enforce 15-minute expiry and token revocation.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Resolve the signing key via the centralized static helper on TokenService so the
        // same secret-resolution logic (config → env var → dev fallback) is used here and
        // in the scoped TokenService instance that generates tokens.
        var signingKey = TokenService.ResolveSigningKey(builder.Configuration);
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "nodpt-api";
        var audience = builder.Configuration["Jwt:Audience"] ?? "nodpt-client";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            // Zero clock-skew: the 15-minute access token window is tight
            ClockSkew = TimeSpan.Zero,
        };

        options.IncludeErrorDetails = builder.Environment.IsDevelopment();

        options.Events = new JwtBearerEvents
        {
            // Check whether the access token has been revoked (e.g., after logout).
            // Revoked JTIs are stored in Redis with a TTL equal to the token's
            // remaining validity and are therefore auto-cleaned without a cron job.
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?
                    .FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    var tokenService = context.HttpContext.RequestServices
                        .GetRequiredService<TokenService>();

                    if (await tokenService.IsAccessTokenRevokedAsync(jti))
                    {
                        context.Fail("Token has been revoked.");
                    }
                }
            },

            // Allow SignalR to pass the access token via query-string parameters
            // "access_token" or "token" when connecting to the /signalr hub.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (string.IsNullOrEmpty(accessToken))
                    accessToken = context.Request.Query["token"];

                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/signalr", StringComparison.OrdinalIgnoreCase))
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
Console.WriteLine("SignalR hub mapped successfully to /signalr and /signalR");

app.MapControllers(); // 🔹 Map controllers
app.Run();



























