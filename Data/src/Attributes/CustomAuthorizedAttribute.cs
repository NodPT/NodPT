using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NodPT.Data;
using NodPT.Data.Models;
using NodPT.Data.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal; // Added for JwtRegisteredClaimNames

/// <summary>
/// Custom authorization attribute that optionally checks if the user has admin privileges in the database
/// Usage: [CustomAuthorized] -> requires authenticated user only
/// [CustomAuthorized("Admin")] -> requires authenticated user who is admin
/// </summary>
public class CustomAuthorizedAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public CustomAuthorizedAttribute()
    {
        _role = string.Empty;
    }

    public CustomAuthorizedAttribute(string role)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        string? firebaseUid = UserService.GetFirebaseUIDFromContent(context.HttpContext.User);

        if (string.IsNullOrEmpty(firebaseUid))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "User identifier not found" });
            return;
        }

        // Check database for admin status
        try
        {
            using var dbContext = DatabaseHelper.CreateDbContext();
            if (UserService.IsValidFirebaseUid(firebaseUid, context.HttpContext.User) == false)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "User is not valid" });
                return;
            }

            var dbUser = dbContext.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUid);
            if (dbUser == null)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "User not found" });
                return;
            }

            // Only handle Admin role for now
            if (_role!.Equals("Admin", StringComparison.OrdinalIgnoreCase) && !dbUser.IsAdmin)
            {
                // Unknown role requirement - do not enforce additional checks
                context.Result = new ObjectResult(new { message = "Access denied. Admin privileges required." })
                {
                    StatusCode = 403
                };
                return;
            }

            // User is valid, allow access
        }
        catch (Exception ex)
        {
            context.Result = new StatusCodeResult(500);
            Console.WriteLine($"Error checking admin status: {ex.Message}");
        }
    }




}
