namespace IdentityCore.Models.Interface;

public interface IUser
{
    public Guid Id { get; init; }
    public string Username { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }
    public string ConfirmPassword { get; init; }
}