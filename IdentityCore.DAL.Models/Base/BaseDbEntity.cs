namespace IdentityCore.DAL.Models.Base;

public abstract record BaseDbEntity
{
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}