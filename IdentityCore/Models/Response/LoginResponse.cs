namespace IdentityCore.Models.Response;

public class LoginResponse
{
    public Guid UserId { get; init; }
    public string Bearer { get; init; }
    public string RefreshToken { get; init; }
}