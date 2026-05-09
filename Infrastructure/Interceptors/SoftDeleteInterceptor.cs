using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProviderAssignmentStarter.Domain.Common;
using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> that turns every attempted
/// hard-delete into a soft-delete. Layer 3 of the soft-delete defence-in-
/// depth strategy.
/// </summary>
/// <remarks>
/// <para>
/// The three-layer model (recap):
/// <list type="number">
///   <item><b>Layer 1 — domain contract:</b> <c>ISoftDeletable</c> on <c>BaseEntity</c>. Every aggregate has the columns.</item>
///   <item><b>Layer 2 — global query filter:</b> standard reads silently exclude <c>IsDeleted = true</c>.</item>
///   <item><b>Layer 3 — this interceptor:</b> <c>SaveChanges</c> rewrites <c>EntityState.Deleted</c> into <c>Modified</c> with the soft-delete columns set.</item>
/// </list>
/// </para>
/// <para>
/// What this interceptor does, on every <c>SaveChanges</c>:
/// </para>
/// <list type="number">
///   <item>Snapshot every tracked entity in <c>EntityState.Deleted</c> that implements <c>ISoftDeletable</c>.</item>
///   <item>For each, flip state to <c>Modified</c> and set <c>IsDeleted = true</c>, <c>DeletedDate = UtcNow</c>, <c>DeletedBy = "system"</c>.</item>
///   <item>If the deleted entity is a <c>Provider</c>, cascade the same flip to its <c>Licenses</c> — both the loaded ones and any unloaded ones in the database.</item>
/// </list>
/// <para>
/// The cascade strategy is deliberately two-pronged:
/// </para>
/// <list type="bullet">
///   <item><b>Tracked path:</b> walk <c>provider.Licenses</c> and flip each entry. This is what runs when callers eager-loaded the navigation.</item>
///   <item><b>Bulk path:</b> issue one <c>ExecuteUpdate</c> against the database for any Licenses still flagged not-deleted. This catches Licenses the caller forgot to <c>Include(...)</c>. The two paths overlap harmlessly when both apply.</item>
/// </list>
/// <para>
/// Important: this interceptor is a singleton (registered in <c>Program.cs</c>),
/// stateless, and safe to share across DbContext instances.
/// </para>
/// </remarks>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Hardcoded principal identifier used for the <c>DeletedBy</c> column.
    /// In a real system this would be sourced from <c>HttpContext.User</c>
    /// via an <c>ICurrentUser</c> abstraction passed in via constructor.
    /// Documented in the README as a take-home trade-off.
    /// </summary>
    private const string SystemUser = "system";

    // ---------- Both sync and async entry points share one body ----------

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

    /// <summary>
    /// Walks the change tracker and rewrites every Deleted ISoftDeletable
    /// entry into a Modified entry with the soft-delete columns set.
    /// </summary>
    private static void ApplySoftDelete(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;

        // We MUST snapshot the entries before iterating. Mutating
        // change-tracker state inside an enumeration of
        // ChangeTracker.Entries() throws InvalidOperationException
        // ("collection was modified").
        var deletedEntries = context.ChangeTracker
            .Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in deletedEntries)
        {
            SoftDeleteEntry(entry, now);

            // Cascade: a deleted Provider implicitly takes its Licenses
            // with it. We handle both the tracked and untracked cases
            // inside CascadeToLicenses below.
            if (entry.Entity is Provider provider)
            {
                CascadeToLicenses(context, provider, now);
            }
        }
    }

    /// <summary>
    /// Flips a single ISoftDeletable entry from Deleted → Modified and
    /// stamps the soft-delete columns. Idempotent — safe to call on an
    /// entry that's already Modified (no-op effectively).
    /// </summary>
    private static void SoftDeleteEntry(EntityEntry<ISoftDeletable> entry, DateTime now)
    {
        entry.State = EntityState.Modified;
        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedDate = now;
        entry.Entity.DeletedBy = SystemUser;

        // Mark only the soft-delete columns dirty. We deliberately do not
        // touch other property modification flags so SaveChanges issues
        // a tight UPDATE statement that only writes these three columns.
        // The AuditableInterceptor will additionally stamp ModifiedDate
        // because the entry's State is now Modified.
        entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified = true;
        entry.Property(nameof(ISoftDeletable.DeletedDate)).IsModified = true;
        entry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;
    }

    /// <summary>
    /// Cascade soft-delete from a Provider down to its Licenses.
    /// </summary>
    /// <remarks>
    /// Two complementary mechanisms:
    /// <list type="bullet">
    ///   <item>
    ///     <b>Tracked Licenses</b> (loaded into the navigation) are flipped
    ///     by walking <c>provider.Licenses</c>. With <c>DeleteBehavior.ClientCascade</c>
    ///     these are typically already in Deleted state in the change tracker,
    ///     so this loop is partly redundant but harmless.
    ///   </item>
    ///   <item>
    ///     <b>Untracked Licenses</b> (the caller forgot to <c>Include</c>)
    ///     are flipped by a single <c>ExecuteUpdate</c> against the database.
    ///     This is the safety net that makes <c>_db.Providers.Remove(p)</c>
    ///     "just work" regardless of how the Provider was loaded.
    ///   </item>
    /// </list>
    /// </remarks>
    private static void CascadeToLicenses(DbContext context, Provider provider, DateTime now)
    {
        // (a) Walk loaded Licenses on the navigation collection. The
        //     filter !l.IsDeleted protects against double-processing if
        //     the License was already flipped earlier in the same loop.
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

        // (b) Sweep ANY remaining non-deleted Licenses in the database
        //     for this provider, regardless of whether they were tracked.
        //     ExecuteUpdate runs as an immediate UPDATE statement (not via
        //     the change tracker), so it catches the unloaded case in a
        //     single round-trip.
        //
        //     Note: this runs BEFORE SaveChanges issues its tracked
        //     UPDATEs, so the tracked Licenses we just flipped above
        //     will be written twice (once by ExecuteUpdate against the
        //     DB row, once by the tracked UPDATE). The values are
        //     identical so the redundancy is harmless.
        context.Set<License>()
            .IgnoreQueryFilters()
            .Where(l => l.ProviderId == provider.ProviderId && !l.IsDeleted)
            .ExecuteUpdate(setters => setters
                .SetProperty(l => l.IsDeleted, true)
                .SetProperty(l => l.DeletedDate, now)
                .SetProperty(l => l.DeletedBy, SystemUser));
    }
}
