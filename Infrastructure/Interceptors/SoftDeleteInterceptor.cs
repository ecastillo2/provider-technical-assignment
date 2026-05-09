using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProviderAssignmentStarter.Domain.Common;
using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Interceptors;

/// <summary>
/// EF Core SaveChangesInterceptor that enforces the no-hard-delete rule
/// at the persistence layer.
///
/// Layer 3 of the soft-delete defence-in-depth strategy:
///   1. ISoftDeletable interface         - typed contract on entities
///   2. Global query filter (DbContext)  - excludes deleted rows from reads
///   3. This interceptor                 - rewrites Deleted -> Modified
///
/// Behaviour:
///  - Every entity flagged EntityState.Deleted is converted to Modified
///    and its IsDeleted/DeletedDate/DeletedBy fields are populated.
///  - When the deleted entity is a Provider, its loaded Licenses are
///    soft-deleted in the same SaveChanges call (cascade soft-delete).
///  - Unloaded child Licenses are flipped via a single UPDATE statement
///    so the cascade still works when the navigation isn't eager-loaded.
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private const string SystemUser = "system";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplySoftDelete(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;

        // Snapshot the deleted entries first; rewriting state during
        // iteration over ChangeTracker.Entries() is unsafe.
        var deletedEntries = context.ChangeTracker
            .Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in deletedEntries)
        {
            SoftDeleteEntry(entry, now);

            // Cascade: when a Provider is deleted, soft-delete its licenses.
            if (entry.Entity is Provider provider)
            {
                CascadeToLicenses(context, provider, now);
            }
        }
    }

    private static void SoftDeleteEntry(EntityEntry<ISoftDeletable> entry, DateTime now)
    {
        entry.State = EntityState.Modified;
        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedDate = now;
        entry.Entity.DeletedBy = SystemUser;

        // Don't bump audit fields when soft-deleting; only the soft-delete
        // columns should change. Mark only the soft-delete columns dirty.
        entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified = true;
        entry.Property(nameof(ISoftDeletable.DeletedDate)).IsModified = true;
        entry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;
    }

    private static void CascadeToLicenses(DbContext context, Provider provider, DateTime now)
    {
        // a) Licenses already tracked / loaded into the navigation collection
        foreach (var license in provider.Licenses.Where(l => !l.IsDeleted))
        {
            var licenseEntry = context.Entry(license);
            licenseEntry.State = EntityState.Modified;
            license.IsDeleted = true;
            license.DeletedDate = now;
            license.DeletedBy = SystemUser;

            licenseEntry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified = true;
            licenseEntry.Property(nameof(ISoftDeletable.DeletedDate)).IsModified = true;
            licenseEntry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;
        }

        // b) Licenses NOT loaded into memory: flip them with a single
        //    UPDATE so callers don't have to remember to eager-load
        //    Licenses before deleting a Provider. ExecuteUpdate runs
        //    outside the change tracker, so it's safe to call here.
        context.Set<License>()
            .IgnoreQueryFilters()
            .Where(l => l.ProviderId == provider.ProviderId && !l.IsDeleted)
            .ExecuteUpdate(setters => setters
                .SetProperty(l => l.IsDeleted, true)
                .SetProperty(l => l.DeletedDate, now)
                .SetProperty(l => l.DeletedBy, SystemUser));
    }
}
