using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NodPT.Data.Models;
using NodPT.Data.Services;

public class UserService
{
    // verify if user is active, approved, and not banned
    public static bool IsUserValid(string firebaseUId, NodPTDbContext context)
    {
        try
        {
            User? user = context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUId);
            return user != null && user.Active && user.Approved && !user.Banned;
        }
        catch (Exception ex)
        {
            LogService.LogError(ex, firebaseUId, "IsUserValid");
        }

        return false;
    }

    /// <summary>
    /// get user by firebaseUId if active, approved, and not banned
    /// </summary>
    /// <param name="firebaseUId"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static User? GetUser(string firebaseUId, NodPTDbContext context)
    {
        try
        {
            var user = context.Users.FirstOrDefault(u => u.FirebaseUid == firebaseUId);
            if (user != null && user.Active && user.Approved && !user.Banned)
                return user;
        }
        catch (Exception ex)
        {
            LogService.LogError(ex, firebaseUId, "GetUser");
        }
        return null;
    }

    // UserService implementation
    public static string? GetFirebaseUIDFromContent(ClaimsPrincipal user)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        // Get Firebase UID from claims (Firebase ID token contains sub and user_id; sub is mapped to ClaimTypes.NameIdentifier by default)
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("user_id")?.Value
                           ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? user.FindFirst("sub")?.Value;

    }

    /// <summary>
    /// check validity of firebaseUid against the logged in user
    /// </summary>
    /// <param name="firebaseUid"></param>
    /// <param name="User"></param>
    /// <returns></returns>
    public static bool IsValidFirebaseUid(string? firebaseUid, ClaimsPrincipal User)
    {
        if (User.Identity == null)
        {
            return false;
        }

        if (User.Identity.IsAuthenticated == false)
        {
            return false;
        }
        string? currentFbUID = GetFirebaseUIDFromContent(User);
        return !string.IsNullOrEmpty(currentFbUID) && currentFbUID.Equals(firebaseUid, StringComparison.OrdinalIgnoreCase);
    }
}