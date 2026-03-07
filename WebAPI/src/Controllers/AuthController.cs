using Microsoft.AspNetCore.Mvc;
using DevExpress.Xpo;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using NodPT.Data.DTOs;
using NodPT.Data.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Linq;
using NodPT.API.Services;

namespace NodPT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        UnitOfWork? session;
        private readonly TokenService _tokenService;

        public AuthController(UnitOfWork _unitOfWork, TokenService tokenService)
        {
            this.session = _unitOfWork;
            _tokenService = tokenService;
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
                User? user = null;
                bool isNewUser = false;

#if DEBUG
                // In Development mode, bypass Firebase validation and return first active user
                Console.WriteLine("DEBUG MODE: Bypassing Firebase authentication in AuthController");

                session!.BeginTransaction();

                // Find first active, approved, non-banned user
                user = session.Query<User>()
                    .Where(u => u.Active && u.Approved && !u.Banned)
                    .FirstOrDefault();

                if (user == null)
                {
                    // If no valid user exists, create a development user
                    user = CreateNewUser(session,
                        "dev-user",
                        "dev@example.com",
                        "Development User",
                        null,
                        true);
                    isNewUser = true;
                    Console.WriteLine($"DEBUG MODE: No valid user found, created development user with FirebaseUid 'dev-user' {isNewUser}");
                }
                else
                {
                    // Update last login time for existing user
                    user.LastLoginAt = DateTime.UtcNow;
                }
#else
                // Validate Firebase token using Firebase Admin SDK
                var firebaseUserInfo = await NodPT.API.Services.FirebaseService.ValidateFirebaseTokenAsync(request.FirebaseToken);
                if (firebaseUserInfo == null)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid Firebase token"
                    });
                }

                session!.BeginTransaction();

                // Find or create user
                user = session.FindObject<User>(new DevExpress.Data.Filtering.BinaryOperator("FirebaseUid", firebaseUserInfo.Uid));

                if (user == null)
                {
                    // Auto-create user if not exists - defaults to Approved=false, Banned=false
                    user = CreateNewUser(session, 
                        firebaseUserInfo.Uid, 
                        firebaseUserInfo.Email, 
                        firebaseUserInfo.DisplayName, 
                        firebaseUserInfo.PhotoUrl, 
                        false);
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
                        session.Save(user);
                        session.CommitTransaction();
                    }
                    else
                    {
                        session.RollbackTransaction();
                    }

                    await LogUserAccessAsync(user, "login", false, validationError.Message);
                    return Unauthorized(validationError);
                }
#endif

                // Invalidate any existing refresh tokens (clear DB fields + Redis)
                if (!string.IsNullOrEmpty(user.RefreshToken))
                {
                    await _tokenService.RevokeRefreshTokenAsync(user.FirebaseUid!, user.RefreshToken);
                }
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;

                session.Save(user);
                session.CommitTransaction();

                // Generate HMAC-SHA256 signed access token (15 min) and refresh token (30 days)
                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = TokenService.GenerateRefreshToken();
                await _tokenService.StoreRefreshTokenAsync(refreshToken, user.FirebaseUid!);

#if !DEBUG
                // Log successful login
                await LogUserAccessAsync(user, "login", true);
#endif
                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    User = new UserDto
                    {
                        Oid = user.Oid,
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
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.Add(TokenService.AccessTokenExpiry)
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
                // Look up the firebaseUid that owns this refresh token from Redis
                var firebaseUid = await _tokenService.GetRefreshTokenOwnerAsync(request.RefreshToken);

                if (string.IsNullOrEmpty(firebaseUid))
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid or expired refresh token"
                    });
                }

                session!.BeginTransaction();

                // Load the user from the database using the firebaseUid stored in Redis
                var user = session.FindObject<User>(new DevExpress.Data.Filtering.BinaryOperator("FirebaseUid", firebaseUid));

                if (user == null || !user.Active)
                {
                    session.RollbackTransaction();
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
                    session.RollbackTransaction();
                    await LogUserAccessAsync(user, "refresh_token", false, validationError.Message);
                    return Unauthorized(validationError);
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                session.Save(user);
                session.CommitTransaction();

                // Rotate: revoke old refresh token and issue new ones
                await _tokenService.RevokeRefreshTokenAsync(firebaseUid, request.RefreshToken);
                var newRefreshToken = TokenService.GenerateRefreshToken();
                await _tokenService.StoreRefreshTokenAsync(newRefreshToken, firebaseUid);

                var newAccessToken = _tokenService.GenerateAccessToken(user);

                // Log successful token refresh
                await LogUserAccessAsync(user, "refresh_token", true);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    User = new UserDto
                    {
                        Oid = user.Oid,
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
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.Add(TokenService.AccessTokenExpiry)
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
        /// Logout and invalidate the current access token and refresh token
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

                // Revoke the current access token JTI so it cannot be reused.
                // The revoked entry in Redis auto-expires when the token would have
                // expired naturally (≤ 15 min), avoiding any memory accumulation.
                var rawToken = Request.Headers["Authorization"].ToString()
                    .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

                if (!string.IsNullOrEmpty(rawToken))
                {
                    var (jti, expiry) = _tokenService.ParseTokenClaims(rawToken);
                    if (!string.IsNullOrEmpty(jti) && expiry.HasValue)
                    {
                        await _tokenService.RevokeAccessTokenAsync(jti, expiry.Value);
                    }
                }

                // Remove the user's active refresh token from Redis so it cannot be reused.
                session!.BeginTransaction();
                var user = session.FindObject<User>(new DevExpress.Data.Filtering.BinaryOperator("FirebaseUid", uid));
                if (user != null)
                {
                    // Clear any legacy DB refresh-token field
                    if (!string.IsNullOrEmpty(user.RefreshToken))
                    {
                        user.RefreshToken = null;
                        session.Save(user);
                    }
                    session.CommitTransaction();
                }
                else
                {
                    session.RollbackTransaction();
                }

                // Revoke refresh token stored in Redis (keyed by uid, regardless of DB state)
                await _tokenService.RevokeRefreshTokenByUserAsync(uid);

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
        /// Create a new user with the specified details
        /// </summary>
        /// <param name="session">Database session</param>
        /// <param name="firebaseUid">Firebase UID</param>
        /// <param name="email">User email</param>
        /// <param name="displayName">User display name</param>
        /// <param name="photoUrl">User photo URL</param>
        /// <param name="approved">Whether the user is approved</param>
        /// <returns>The newly created user</returns>
        private User CreateNewUser(UnitOfWork session, string firebaseUid, string? email, string? displayName, string? photoUrl, bool approved)
        {
            var user = new User(session)
            {
                FirebaseUid = firebaseUid,
                Email = email,
                DisplayName = displayName,
                PhotoUrl = photoUrl,
                Active = true,
                Approved = approved,
                Banned = false,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            session.Save(user);
            return user;
        }

        /// <summary>
        /// Log user access activity
        /// </summary>
        private async Task LogUserAccessAsync(User? user, string action, bool success, string? errorMessage = null)
        {

            if (user == null || session == null)
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
                var freshUser = session!.GetObjectByKey<User>(user.Oid);
                if (freshUser == null)
                    return;

                session.BeginTransaction();
                freshUser.AccessLogs.Add(new UserAccessLog(session)
                {
                    User = freshUser,
                    Action = action,
                    IpAddress = ip,
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Success = success,
                    ErrorMessage = errorMessage
                });

                this.session.Save(freshUser);
                await this.session.CommitTransactionAsync();
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