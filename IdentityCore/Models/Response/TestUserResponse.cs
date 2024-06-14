namespace IdentityCore.Models.Response;

public class TestUserResponse
{
    public Guid Id { get; init; }
    public string Username { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }
}