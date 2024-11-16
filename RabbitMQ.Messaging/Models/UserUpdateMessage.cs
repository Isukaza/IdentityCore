using IdentityCore.DAL.PostgreSQL.Models.enums;
using MessagePack;

namespace RabbitMQ.Messaging.Models;

[MessagePackObject]
public class UserUpdateMessage
{
    [Key(0)]
    public required string UserEmail { get; init; }
    [Key(1)]
    public required string UserName { get; init; }
    [Key(2)]
    public string OldValue { get; set; }
    [Key(3)]
    public string NewValue { get; set; }
    [Key(4)]
    public required TokenType ChangeType { get; init; }
    [Key(5)]
    public required string ConfirmationLink { get; init; }
}