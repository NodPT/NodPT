using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;

namespace NodPT.API.Authentication;

public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ExecutorClientId = "executor-client";

    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for executor client special case
        if (Request.Query.TryGetValue("clientType", out var clientType) && 
            clientType == ExecutorClientId)
        {
            var executorClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ExecutorClientId),
                new Claim(ClaimTypes.Name, "Executor Client"),
                new Claim("client_type", "executor")
            };

            var executorIdentity = new ClaimsIdentity(executorClaims, Scheme.Name);
            var executorPrincipal = new ClaimsPrincipal(executorIdentity);
            var executorTicket = new AuthenticationTicket(executorPrincipal, Scheme.Name);

            return AuthenticateResult.Success(executorTicket);
        }

        // Extract the token from the Authorization header or query string
        string? token = null;

        if (Request.Headers.ContainsKey("Authorization"))
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }
        }

        // For SignalR connections, check the access_token query parameter
        if (string.IsNullOrEmpty(token) && Request.Query.ContainsKey("access_token"))
        {
            token = Request.Query["access_token"];
        }

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("No token provided");
        }

        try
        {
            // Check if Firebase is properly configured
            if (FirebaseAuth.DefaultInstance == null)
            {
                // Only allow fallback in development environment
                var isDevelopment = Context.Request.HttpContext.RequestServices
                    .GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()?
                    .IsDevelopment() ?? false;

                if (!isDevelopment)
                {
                    Logger.LogError("Firebase not configured in production environment. Authentication failed.");
                    return AuthenticateResult.Fail("Authentication service not configured");
                }

                // Development mode: Allow connection without token validation
                Logger.LogWarning("Firebase not configured. Allowing connection in development mode only.");
                
                var devClaims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "dev-user-" + Guid.NewGuid().ToString("N").Substring(0, 8)),
                    new Claim(ClaimTypes.Name, "Development User"),
                    new Claim("client_type", "development")
                };

                var devIdentity = new ClaimsIdentity(devClaims, Scheme.Name);
                var devPrincipal = new ClaimsPrincipal(devIdentity);
                var devTicket = new AuthenticationTicket(devPrincipal, Scheme.Name);

                return AuthenticateResult.Success(devTicket);
            }

            // Verify the Firebase token
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, decodedToken.Uid),
                new Claim(ClaimTypes.Name, GetClaimValueAsString(decodedToken.Claims, "name") ?? decodedToken.Uid),
                new Claim(ClaimTypes.Email, GetClaimValueAsString(decodedToken.Claims, "email") ?? ""),
                new Claim("firebase_uid", decodedToken.Uid),
                new Claim("client_type", "regular")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (FirebaseAuthException ex)
        {
            Logger.LogWarning(ex, "Firebase authentication failed");
            return AuthenticateResult.Fail($"Invalid token: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Authentication error");
            return AuthenticateResult.Fail("Authentication error");
        }
    }

    private static string? GetClaimValueAsString(IReadOnlyDictionary<string, object> claims, string key)
    {
        if (claims.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }
}
