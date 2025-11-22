using DevExpress.Data.Filtering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using NodPT.Data.Models;
using DevExpress.Xpo;

namespace NodPT.API.Controllers
{
    [CustomAuthorized]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {

        private UnitOfWork session;

        public UsersController(UnitOfWork _session)
        {
            session = _session;
        }


        [HttpGet]
        [CustomAuthorized("Admin")]
        public IActionResult GetUsers()
        {

            var users = new XPCollection<User>(session);

            // Project to DTOs to avoid serialization issues
            var userDtos = users.Select(u => new
            {
                u.Oid,
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

            var user = session.FindObject<User>(new BinaryOperator("FirebaseUid", firebaseUid));

            if (user == null) return NotFound();

            var userDto = new
            {
                user.Oid,
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
        public IActionResult CreateUser([FromBody] CreateUserRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.FirebaseUid))
                return BadRequest("FirebaseUid is required");



            // Check if user already exists
            var existingUser = session.FindObject<User>(new BinaryOperator("FirebaseUid", request.FirebaseUid));
            if (existingUser != null)
                return Conflict("User already exists");

            var user = new User(session)
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

            session.Save(user);
            session.CommitTransaction();

            return CreatedAtAction(nameof(GetUser), new { firebaseUid = user.FirebaseUid }, user);
        }

        [CustomAuthorized("Admin")]
        public IActionResult UpdateUserStatus(string firebaseUid, [FromBody] UpdateUserStatusRequest request)
        {

            session.BeginTransaction();

            var user = session.FindObject<User>(new BinaryOperator("FirebaseUid", firebaseUid));

            if (user == null)
            {
                session.RollbackTransaction();
                return NotFound();
            }

            user.Active = request.Active;
            session.Save(user);
            session.CommitTransaction();

            return Ok(user);
        }

        [CustomAuthorized("Admin")]
        public IActionResult ApproveUser(string firebaseUid, [FromBody] UpdateUserApprovalRequest request)
        {

            session.BeginTransaction();

            var user = session.FindObject<User>(new BinaryOperator("FirebaseUid", firebaseUid));

            if (user == null)
            {
                session.RollbackTransaction();
                return NotFound();
            }

            user.Approved = request.Approved;
            session.Save(user);
            session.CommitTransaction();

            return Ok(new { message = request.Approved ? "User approved successfully" : "User approval revoked", user });
        }

        [CustomAuthorized("Admin")]
        public IActionResult BanUser(string firebaseUid, [FromBody] UpdateUserBanRequest request)
        {

            session.BeginTransaction();

            var user = session.FindObject<User>(new BinaryOperator("FirebaseUid", firebaseUid));

            if (user == null)
            {
                session.RollbackTransaction();
                return NotFound();
            }

            user.Banned = request.Banned;
            session.Save(user);
            session.CommitTransaction();

            return Ok(new { message = request.Banned ? "User banned successfully" : "User unbanned successfully", user });
        }

        /// <summary>
        /// allowed user to update their own profile
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("me")]
        public IActionResult UpdateMyProfile([FromBody] UpdateUserRequest request)
        {
            // Get user from token
            var user = UserService.GetUser(User, session);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found or invalid" });
            }

            session.BeginTransaction();

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
                        session.RollbackTransaction();
                        return StatusCode(403, new { message = "Only administrators can change email addresses." });
                    }
                }

                session.Save(user);
                session.CommitTransaction();

                return Ok(new { message = "User updated successfully", user });
            }
            catch (Exception ex)
            {
                session.RollbackTransaction();
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