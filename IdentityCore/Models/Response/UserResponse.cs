namespace IdentityCore.Models.Response;

public record UserResponse
{
    public Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
}