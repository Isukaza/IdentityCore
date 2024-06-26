namespace IdentityCore.Models.Request;

public record LogoutRequest
{
    public string RefreshToken { get; set; }
}