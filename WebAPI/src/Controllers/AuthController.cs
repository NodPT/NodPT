using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using NodPT.Data.DTOs;
using NodPT.Data.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Linq;
using NodPT.Data;

namespace NodPT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly NodPTDbContext _context;
        
        public AuthController(NodPTDbContext context)
        {
            _context = context;
        }

        private static readonly HttpClient httpClient = new();

        /// <summary>
        /// Login with Firebase token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.FirebaseToken))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Firebase token is required"
                });
            }

            try
            {
                // Validate Firebase token using Firebase Admin SDK (with DEBUG mock fallback)
                var firebaseUserInfo = await NodPT.API.Services.FirebaseService.ValidateFirebaseTokenAsync(request.FirebaseToken);
                if (firebaseUserInfo == null)
                {
                    // LogUserAccess(null, null, "login", false, "Invalid Firebase token");
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid Firebase token"
                    });
                }

                // Find or create user
                var user = _context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUserInfo.Uid);

                bool isNewUser = false;
                if (user == null)
                {
                    // Auto-create user if not exists - defaults to Approved=false, Banned=false
                    user = new User
                    {
                        FirebaseUid = firebaseUserInfo.Uid,
                        Email = firebaseUserInfo.Email,
                        DisplayName = firebaseUserInfo.DisplayName,
                        PhotoUrl = firebaseUserInfo.PhotoUrl,
                        Active = true,
                        Approved = false,
                        Banned = false,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    isNewUser = true;
                }
                else
                {
                    // Update last login time for existing users
                    user.LastLoginAt = DateTime.UtcNow;
                    user.FirebaseUid = firebaseUserInfo.Uid;
                }

                // Validate user status (banned and approved checks)
                var validationError = ValidateUserStatus(user);
                if (validationError != null)
                {
                    // Save new user even if not approved, so account is created
                    if (isNewUser)
                    {
                        await _context.SaveChangesAsync();
                    }

                    await LogUserAccessAsync(user, "login", false, validationError.Message);
                    return Unauthorized(validationError);
                }

                // Generate refresh token if remember me is enabled
                string? refreshToken = null;
                if (request.RememberMe)
                {
                    refreshToken = GenerateRefreshToken();
                    user.RefreshToken = refreshToken;
                }

                await _context.SaveChangesAsync();

                // Log successful login
                await LogUserAccessAsync(user, "login", true);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    User = new UserDto
                    {
                        Oid = user.Id,
                        FirebaseUid = user.FirebaseUid,
                        Email = user.Email,
                        DisplayName = user.DisplayName,
                        PhotoUrl = user.PhotoUrl,
                        Active = user.Active,
                        Approved = user.Approved,
                        Banned = user.Banned,
                        IsAdmin = user.IsAdmin,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    },
                    AccessToken = request.FirebaseToken, // In real implementation, generate JWT
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1) // Mock expiration
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Refresh token is required"
                });
            }

            try
            {
                // Find user by refresh token
                var user = _context.Users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);

                if (user == null || !user.Active)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid or expired refresh token"
                    });
                }

                // Validate user status (banned and approved checks)
                var validationError = ValidateUserStatus(user);
                if (validationError != null)
                {
                    await LogUserAccessAsync(user, "refresh_token", false, validationError.Message);
                    return Unauthorized(validationError);
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;

                // Generate new refresh token
                var newRefreshToken = GenerateRefreshToken();
                user.RefreshToken = newRefreshToken;

                await _context.SaveChangesAsync();

                // Log successful token refresh
                await LogUserAccessAsync(user, "refresh_token", true);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    User = new UserDto
                    {
                        Oid = user.Id,
                        FirebaseUid = user.FirebaseUid,
                        Email = user.Email,
                        DisplayName = user.DisplayName,
                        PhotoUrl = user.PhotoUrl,
                        Active = user.Active,
                        Approved = user.Approved,
                        Banned = user.Banned,
                        IsAdmin = user.IsAdmin,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    },
                    AccessToken = $"mock_token_{user.FirebaseUid}", // Mock token
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Logout and invalidate refresh token
        /// </summary>
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                string? uid = UserService.GetFirebaseUIDFromContent(User);

                if (string.IsNullOrEmpty(uid))
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid user token"
                    });
                }

                // Find user by the user who was login with the jwt token
                var user = _context.Users.FirstOrDefault(u => u.FirebaseUid == uid);

                if (user != null)
                {
                    // Clear refresh token
                    user.RefreshToken = null;
                    await _context.SaveChangesAsync();

                    // Log successful logout
                    // LogUserAccess(user, "logout", true);
                }

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Logout successful"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        #region Private Methods

        /// <summary>
        /// Validate user status (banned and approved checks)
        /// </summary>
        /// <returns>Null if validation passes, otherwise returns an AuthResponseDto with error details</returns>
        private AuthResponseDto? ValidateUserStatus(User user)
        {
            // Check if user is banned
            if (user.Banned)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Your account has been banned. Please contact the site administrator."
                };
            }

            // Check if user is approved
            if (!user.Approved)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Your account is pending approval. Please wait for the site administrator to approve your account."
                };
            }

            return null;
        }

        /// <summary>
        /// Generate a secure refresh token
        /// </summary>
        private string GenerateRefreshToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[64];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData);
        }

        /// <summary>
        /// Log user access activity
        /// </summary>
        private async Task LogUserAccessAsync(User? user, string action, bool success, string? errorMessage = null)
        {

            if (user == null)
                return;

            // Console log for immediate feedback
            var userInfo = user != null ? $"User: {user.FirebaseUid}" : "User: None";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var ip = GetClientIpAddress();

            Console.WriteLine($"[{timestamp}] {action.ToUpper()} - {userInfo} - IP: {ip} - Success: {success}" +
                              (errorMessage != null ? $" - Error: {errorMessage}" : ""));

            // Database logging in background task to avoid transaction conflicts
            try
            {
                // Use a separate context for logging to avoid transaction conflicts
                using var logContext = DatabaseHelper.CreateDbContext();
                
                var freshUser = logContext.Users.FirstOrDefault(u => u.Id == user.Id);
                if (freshUser == null)
                    return;

                var accessLog = new UserAccessLog
                {
                    UserId = freshUser.Id,
                    Action = action,
                    IpAddress = ip,
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Success = success,
                    ErrorMessage = errorMessage
                };

                logContext.UserAccessLogs.Add(accessLog);
                await logContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log user access to database: {ex.Message}");
            }
        }

        /// <summary>
        /// Get client IP address
        /// </summary>
        private string? GetClientIpAddress()
        {
            return Request.Headers.ContainsKey("X-Forwarded-For")
                ? Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                : HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        #endregion

        #region Helper Classes

        private class FirebaseUserInfo
        {
            public string? Uid { get; set; }
            public string? Email { get; set; }
            public string? DisplayName { get; set; }
            public string? PhotoUrl { get; set; }
        }

        #endregion
    }
}