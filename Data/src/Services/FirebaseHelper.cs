using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace NodPT.Data.Services
{
    public class FirebaseHelper
    {
        // Helper to obtain Firebase signing keys with simple cache
        public static class FirebaseKeysProvider
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
    }
}