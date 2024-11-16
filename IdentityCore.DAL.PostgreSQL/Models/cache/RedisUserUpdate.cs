using System.Text.Json.Serialization;
using IdentityCore.DAL.PostgreSQL.Models.enums;

namespace IdentityCore.DAL.PostgreSQL.Models.cache;

public class RedisUserUpdate
{
    public required Guid Id { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? NewValue { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Salt { get; set; }

    public required TokenType ChangeType { get; set; }
}