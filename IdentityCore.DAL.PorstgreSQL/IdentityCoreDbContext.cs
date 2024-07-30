using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

using IdentityCore.DAL.Models;

namespace IdentityCore.DAL.PorstgreSQL;

public class IdentityCoreDbContext : DbContext
{
    #region Fileds

    private readonly ILogger<IdentityCoreDbContext> _logger;

    #endregion
    
    #region C-tor
    
    public IdentityCoreDbContext(DbContextOptions<IdentityCoreDbContext> options, ILogger<IdentityCoreDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    #endregion
    
    #region DbSet

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<RegistrationToken> RegistrationTokens { get; set; }

    #endregion
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region Users

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        #endregion

        #region Users + RefreshTokens

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion
        
        #region Users + RegistrationTokens

        modelBuilder.Entity<RegistrationToken>()
            .HasOne(rt => rt.User)
            .WithOne(u => u.RegistrationTokens)
            .HasForeignKey<RegistrationToken>(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion
    }
    
    #region SaveFnction

    public bool SaveAndCompareAffectedRows(bool log = true)
    {
        try
        {
            var affectedRows = GetAffectedRows();
            return SaveChanges() >= affectedRows.Count;
        }
        catch (DbUpdateException ex)
        {
            return log && LogException(ex);
        }
    }

    public async Task<bool> SaveAndCompareAffectedRowsAsync(bool log = true)
    {
        try
        {
            var affectedRows = GetAffectedRows();
            return await SaveChangesAsync() >= affectedRows.Count;
        }
        catch (DbUpdateException ex)
        {
            return log && LogException(ex);
        }
    }

    private List<EntityEntry> GetAffectedRows()
    {
        var affectedRows = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added
                        || e.State == EntityState.Modified
                        || e.State == EntityState.Deleted)
            .ToList();
        
        foreach (var affectedRow in affectedRows)
        {
            var property = affectedRow.Properties.FirstOrDefault(p => p.Metadata.Name == "Modified");
            if (property != null)
                property.CurrentValue = DateTime.UtcNow;
        }

        return affectedRows;
    }
    
    private bool LogException(DbUpdateException ex)
    {
        _logger.LogInformation("[DB] Exception during saving {ex}", ex);
        _logger.LogInformation("Entries: ");
        foreach (var entry in ex.Entries)
            _logger.LogInformation("\t{Type} {EntryState}", entry.Entity.GetType(), entry.State);

        var st = new StackTrace(true);
        var frameDescriptions = st
            .GetFrames()
            .Where(f => !string.IsNullOrEmpty(f.GetFileName()))
            .Select(f => $"at {f.GetMethod()} in {f.GetFileName()}:line {f.GetFileLineNumber()}")
            .ToList();
        _logger.LogInformation("[DB] Exception stacktrace: {Frames}", string.Join(Environment.NewLine, frameDescriptions));
        return false;
    }

    #endregion
}