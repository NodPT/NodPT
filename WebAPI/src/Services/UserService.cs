using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class UserService
{
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