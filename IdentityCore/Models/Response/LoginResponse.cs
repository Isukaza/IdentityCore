namespace IdentityCore.Models.Response;

public class LoginResponse
{
    public string Bearer { get; set; }
    public string RefreshToken { get; set; }
}