using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProviderAssignmentStarter.Domain.Common;

namespace ProviderAssignmentStarter.Infrastructure.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> that stamps audit
/// timestamps on every entity that implements <see cref="IAuditable"/>.
/// </summary>
/// <remarks>
/// <para>
/// What gets stamped:
/// <list type="bullet">
///   <item><b>On insert</b> (state == Added): <c>CreatedDate = UtcNow</c>.</item>
///   <item><b>On update</b> (state == Modified): <c>ModifiedDate = UtcNow</c>, AND <c>CreatedDate</c> is explicitly marked as not-modified so it cannot be overwritten by accident.</item>
///   <item><b>On soft-delete:</b> the <c>SoftDeleteInterceptor</c> rewrites the state to Modified before this runs, so soft-deletes also bump <c>ModifiedDate</c>. This is intentional — a soft-delete IS a modification of the row.</item>
/// </list>
/// </para>
/// <para>
/// Both interceptors are registered as singletons in <c>Program.cs</c>.
/// They are stateless and safe to share across DbContext instances.
/// </para>
/// </remarks>
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

    /// <summary>
    /// Walks every IAuditable entry in the change tracker and stamps the
    /// appropriate timestamps based on the entity's current state.
    /// </summary>
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
                    // Defensive: if a caller (or a misbehaving mapper)
                    // tried to write CreatedDate, drop the change. This
                    // is the cheapest place to enforce immutability of
                    // CreatedDate after insert.
                    entry.Property(nameof(IAuditable.CreatedDate)).IsModified = false;
                    break;

                // Other states (Detached, Unchanged, Deleted) are
                // intentionally ignored. SoftDeleteInterceptor rewrites
                // Deleted → Modified BEFORE we run if both interceptors
                // are wired in the order Program.cs uses, so a true
                // Deleted state should never reach here.
            }
        }
    }
}
