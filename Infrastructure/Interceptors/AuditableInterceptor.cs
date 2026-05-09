using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProviderAssignmentStarter.Domain.Common;

namespace ProviderAssignmentStarter.Infrastructure.Interceptors;

/// <summary>
/// Stamps CreatedDate / ModifiedDate on entities that implement IAuditable
/// without requiring callers to remember. UTC is used so values are
/// timezone-independent; views render in local time.
/// </summary>
public class AuditableInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StampAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void StampAudit(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedDate = now;
                    // Never let a caller overwrite CreatedDate after insert.
                    entry.Property(nameof(IAuditable.CreatedDate)).IsModified = false;
                    break;
            }
        }
    }
}
