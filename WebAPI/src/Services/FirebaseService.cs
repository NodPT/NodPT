using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using System.Text.RegularExpressions; // Added for UID sanitization

namespace NodPT.API.Services
{
    public class FirebaseService
    {
        private static readonly HttpClient httpClient = new HttpClient();

        static FirebaseService()
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

        public static async Task<FirebaseUserInfo?> ValidateFirebaseTokenAsync(string token)
        {
            try
            {
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                return new FirebaseUserInfo
                {
                    Uid = SanitizeText(decodedToken.Uid),
                    Email = SanitizeText(decodedToken.Claims.ContainsKey("email") ? decodedToken.Claims["email"].ToString() : null),
                    DisplayName = SanitizeText(decodedToken.Claims.ContainsKey("name") ? decodedToken.Claims["name"].ToString() : null),
                    PhotoUrl = SanitizeText(decodedToken.Claims.ContainsKey("picture") ? decodedToken.Claims["picture"].ToString() : null)
                };
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"Firebase token validation failed: {ex.Message}");
                return null;
            }
        }

        public static string? SanitizeText(string? uid)
        {
            if (string.IsNullOrEmpty(uid)) return uid;
            // Remove control chars and trim
            uid = uid.Trim();
            // Keep only allowed Firebase UID safe chars (alphanumeric, '-', '_', ':')
            uid = Regex.Replace(uid, "[^A-Za-z0-9_:\\-]", string.Empty);
            return uid;
        }
    }

    public class FirebaseUserInfo
    {
        public string? Uid { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
