namespace IdentityCore.Models.Response;

public class LoginResponse
{
    public Guid UserId { get; set; }
    public string Bearer { get; set; }
    public string RefreshToken { get; set; }
}