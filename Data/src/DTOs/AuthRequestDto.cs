namespace NodPT.Data.DTOs
{
    public class LoginRequestDto
    {
        public string? FirebaseToken { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        public string? RefreshToken { get; set; }
    }

    public class LogoutRequestDto
    {
        public string? RefreshToken { get; set; }
    }
}