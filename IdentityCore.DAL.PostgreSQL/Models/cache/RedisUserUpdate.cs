using System.Text.Json.Serialization;

namespace IdentityCore.DAL.PostgreSQL.Models.cache;

public record RedisUserUpdate
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required Guid Id { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Username { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Email { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Password { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Salt { get; set; }
}