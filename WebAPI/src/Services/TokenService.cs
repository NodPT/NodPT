using Microsoft.IdentityModel.Tokens;
using NodPT.Data.Models;
using RedisService.Cache;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NodPT.API.Services
{
    /// <summary>
    /// Service responsible for generating HMAC-SHA256-signed JWTs, managing
    /// refresh tokens in Redis, and revoking access tokens via Redis TTL.
    ///
    /// Refresh token layout (Redis):
    ///   refresh:token:{token}     → firebaseUid   (TTL 30 days)
    ///   refresh:user:{firebaseUid} → token         (TTL 30 days)
    ///
    /// Revoked access-token layout (Redis):
    ///   revoked:token:{jti}       → "1"           (TTL = remaining validity, ≤ 15 min)
    ///
    /// Old revoked-token entries are automatically purged by Redis TTL so there
    /// is no memory leak caused by accumulating stale entries.
    /// </summary>
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly RedisCacheService _cache;
        private readonly ILogger<TokenService> _logger;

        /// <summary>Access token validity – 15 minutes.</summary>
        public static readonly TimeSpan AccessTokenExpiry = TimeSpan.FromMinutes(15);

        /// <summary>Refresh token validity – 30 days.</summary>
        public static readonly TimeSpan RefreshTokenExpiry = TimeSpan.FromDays(30);

        public TokenService(IConfiguration configuration, RedisCacheService cache, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        // ----------------------------------------------------------------
        // Access-token generation
        // ----------------------------------------------------------------

        /// <summary>
        /// Generate a signed HMAC-SHA256 JWT access token that expires in 15 minutes.
        /// The token carries: sub, email, name, jti and ClaimTypes.NameIdentifier claims.
        /// </summary>
        public string GenerateAccessToken(User user)
        {
            var key = GetSigningKey();
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jti = Guid.NewGuid().ToString();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.FirebaseUid!),
                new Claim(ClaimTypes.NameIdentifier, user.FirebaseUid!),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Name, user.DisplayName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
            };

            var token = new JwtSecurityToken(
                issuer: GetIssuer(),
                audience: GetAudience(),
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.Add(AccessTokenExpiry),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ----------------------------------------------------------------
        // Refresh-token management (Redis)
        // ----------------------------------------------------------------

        /// <summary>
        /// Generate a cryptographically secure opaque refresh token.
        /// </summary>
        public static string GenerateRefreshToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[64];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData);
        }

        /// <summary>
        /// Persist a refresh token in Redis with a 30-day TTL.
        /// Any previously stored token for the same user is overwritten.
        /// </summary>
        public async Task StoreRefreshTokenAsync(string refreshToken, string firebaseUid)
        {
            await _cache.Set($"refresh:token:{refreshToken}", firebaseUid, RefreshTokenExpiry);
            await _cache.Set($"refresh:user:{firebaseUid}", refreshToken, RefreshTokenExpiry);
        }

        /// <summary>
        /// Return the firebaseUid that owns the given refresh token, or null if not found / expired.
        /// </summary>
        public async Task<string?> GetRefreshTokenOwnerAsync(string refreshToken)
        {
            return await _cache.Get($"refresh:token:{refreshToken}");
        }

        /// <summary>
        /// Delete the refresh token for a user from Redis (logout / token rotation).
        /// </summary>
        public async Task RevokeRefreshTokenAsync(string firebaseUid, string refreshToken)
        {
            await _cache.Remove($"refresh:token:{refreshToken}");
            await _cache.Remove($"refresh:user:{firebaseUid}");
        }

        /// <summary>
        /// Look up and delete the active refresh token for a user from Redis using only the
        /// user's firebaseUid. Used during logout when the token value is unknown or unavailable.
        /// </summary>
        public async Task RevokeRefreshTokenByUserAsync(string firebaseUid)
        {
            var token = await _cache.Get($"refresh:user:{firebaseUid}");
            if (!string.IsNullOrEmpty(token))
            {
                await _cache.Remove($"refresh:token:{token}");
            }
            await _cache.Remove($"refresh:user:{firebaseUid}");
        }

        // ----------------------------------------------------------------
        // Access-token revocation (Redis)
        // ----------------------------------------------------------------

        /// <summary>
        /// Revoke an access token by storing its JTI in Redis with a TTL equal to
        /// the token's remaining validity. Redis auto-expires the entry when the
        /// token would have expired naturally, so no cleanup job is required.
        /// </summary>
        public async Task RevokeAccessTokenAsync(string jti, DateTime tokenExpiry)
        {
            var ttl = tokenExpiry - DateTime.UtcNow;
            if (ttl > TimeSpan.Zero)
            {
                await _cache.Set($"revoked:token:{jti}", "1", ttl);
                _logger.LogInformation("Access token {Jti} revoked (expires in {Ttl})", jti, ttl);
            }
        }

        /// <summary>
        /// Returns true when the given JTI has been revoked.
        /// </summary>
        public async Task<bool> IsAccessTokenRevokedAsync(string jti)
        {
            return await _cache.Exists($"revoked:token:{jti}");
        }

        // ----------------------------------------------------------------
        // Token parsing helper
        // ----------------------------------------------------------------

        /// <summary>
        /// Read the JTI and expiry from a raw JWT without re-validating the signature.
        /// Used during logout to obtain the JTI of the caller's current access token.
        /// </summary>
        public (string? jti, DateTime? expiry) ParseTokenClaims(string rawToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(rawToken))
                    return (null, null);

                var jwt = handler.ReadJwtToken(rawToken);
                var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                return (jti, jwt.ValidTo == DateTime.MinValue ? null : jwt.ValidTo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse JWT claims for revocation");
                return (null, null);
            }
        }

        // ----------------------------------------------------------------
        // Configuration helpers
        // ----------------------------------------------------------------

        /// <summary>
        /// Resolve the HMAC signing key from configuration or environment variables.
        /// This static overload is used at startup (before the DI container is built)
        /// so Program.cs and the scoped TokenService instance share the same logic.
        /// </summary>
        public static SymmetricSecurityKey ResolveSigningKey(IConfiguration configuration)
        {
            var secret = configuration["Jwt:Secret"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET")
#if DEBUG
                ?? "nodpt-dev-secret-key-minimum-32-chars!!"
#endif
                ?? throw new InvalidOperationException(
                    "JWT secret key is not configured. Set Jwt:Secret in appsettings or the JWT_SECRET environment variable.");

            if (secret.Length < 32)
                throw new InvalidOperationException("JWT secret key must be at least 32 characters long.");

            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }

        public SymmetricSecurityKey GetSigningKey() => ResolveSigningKey(_configuration);

        public string GetIssuer() => _configuration["Jwt:Issuer"] ?? "nodpt-api";

        public string GetAudience() => _configuration["Jwt:Audience"] ?? "nodpt-client";
    }
}
