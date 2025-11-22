using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NodPT.Data.Models;
using NodPT.Data;
using Microsoft.EntityFrameworkCore;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly NodPTDbContext _context;

        public UsersController(NodPTDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        [CustomAuthorized("Admin")]
        public IActionResult GetUsers()
        {
            var users = _context.Users.ToList();

            // Project to DTOs to avoid serialization issues
            var userDtos = users.Select(u => new
            {
                u.Id,
                u.FirebaseUid,
                u.Email,
                u.DisplayName,
                u.PhotoUrl,
                u.Active,
                u.Approved,
                u.Banned,
                u.IsAdmin,
                u.CreatedAt,
                u.LastLoginAt
            }).ToList();

            return Ok(userDtos);
        }

        [CustomAuthorized("Admin")]
        public IActionResult GetUser(string firebaseUid)
        {
            var user = _context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUid);

            if (user == null) return NotFound();

            var userDto = new
            {
                user.Id,
                user.FirebaseUid,
                user.Email,
                user.DisplayName,
                user.PhotoUrl,
                user.Active,
                user.Approved,
                user.Banned,
                user.IsAdmin,
                user.CreatedAt,
                user.LastLoginAt
            };

            return Ok(userDto);
        }

        [HttpPost]
        [CustomAuthorized("Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.FirebaseUid))
                return BadRequest("FirebaseUid is required");

            // Check if user already exists
            var existingUser = _context.Users.FirstOrDefault(u => u.FirebaseUid == request.FirebaseUid);
            if (existingUser != null)
                return Conflict("User already exists");

            var user = new User
            {
                FirebaseUid = request.FirebaseUid,
                Email = request.Email,
                DisplayName = request.DisplayName,
                PhotoUrl = request.PhotoUrl,
                Active = true,
                Approved = false,
                Banned = false,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { firebaseUid = user.FirebaseUid }, user);
        }

        [CustomAuthorized("Admin")]
        public async Task<IActionResult> UpdateUserStatus(string firebaseUid, [FromBody] UpdateUserStatusRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUid);

            if (user == null)
            {
                return NotFound();
            }

            user.Active = request.Active;
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        [CustomAuthorized("Admin")]
        public async Task<IActionResult> ApproveUser(string firebaseUid, [FromBody] UpdateUserApprovalRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUid);

            if (user == null)
            {
                return NotFound();
            }

            user.Approved = request.Approved;
            await _context.SaveChangesAsync();

            return Ok(new { message = request.Approved ? "User approved successfully" : "User approval revoked", user });
        }

        [CustomAuthorized("Admin")]
        public async Task<IActionResult> BanUser(string firebaseUid, [FromBody] UpdateUserBanRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUid);

            if (user == null)
            {
                return NotFound();
            }

            user.Banned = request.Banned;
            await _context.SaveChangesAsync();

            return Ok(new { message = request.Banned ? "User banned successfully" : "User unbanned successfully", user });
        }

        /// <summary>
        /// allowed user to update their own profile
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserRequest request)
        {
            // Get user from token
            var firebaseUid = User.Claims.FirstOrDefault(c => c.Type == "firebaseUid" || c.Type == "user_id")?.Value;
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return Unauthorized(new { message = "User not found or invalid" });
            }

            var user = _context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUid);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found or invalid" });
            }

            try
            {
                // Update allowed fields
                if (!string.IsNullOrEmpty(request.DisplayName))
                    user.DisplayName = request.DisplayName;

                if (!string.IsNullOrEmpty(request.PhotoUrl))
                    user.PhotoUrl = request.PhotoUrl;

                // Only admins can change email
                if (!string.IsNullOrEmpty(request.Email))
                {
                    if (user.IsAdmin)
                    {
                        user.Email = request.Email;
                    }
                    else
                    {
                        return StatusCode(403, new { message = "Only administrators can change email addresses." });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated successfully", user });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    public class CreateUserRequest
    {
        public string? FirebaseUid { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class UpdateUserStatusRequest
    {
        public bool Active { get; set; }
    }

    public class UpdateUserApprovalRequest
    {
        public bool Approved { get; set; }
    }

    public class UpdateUserBanRequest
    {
        public bool Banned { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? DisplayName { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Email { get; set; }
    }
}