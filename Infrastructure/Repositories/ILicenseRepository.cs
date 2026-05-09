using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Repositories;

/// <summary>
/// Persistence-agnostic contract for the <see cref="License"/> entity.
/// </summary>
/// <remarks>
/// <para>
/// Same patterns as <see cref="IProviderRepository"/>:
/// </para>
/// <list type="bullet">
///   <item>Standard reads go through the global query filter.</item>
///   <item>Mutation goes through the soft-delete interceptor.</item>
///   <item>The service layer depends on this abstraction, not on EF.</item>
/// </list>
/// <para>
/// License does not currently expose its own audit listing because the
/// audit page lives at the Provider aggregate level. If we ever surface
/// "deleted licenses for an active provider" we would add a
/// <c>GetDeletedByProviderAsync</c> here that calls <c>.IgnoreQueryFilters()</c>.
/// </para>
/// </remarks>
public interface ILicenseRepository
{
    /// <summary>Single license by id, with its parent Provider eager-loaded for display.</summary>
    Task<License?> GetByIdAsync(int licenseId, CancellationToken ct = default);

    /// <summary>All non-deleted licenses for a given provider, ordered by expiration.</summary>
    Task<IReadOnlyList<License>> GetByProviderAsync(int providerId, CancellationToken ct = default);

    /// <summary>
    /// Active licenses whose expiration date falls within the next
    /// <paramref name="days"/> window (today through today+days).
    /// Powers the dashboard's "expiring soon" widget.
    /// </summary>
    Task<IReadOnlyList<License>> GetExpiringSoonAsync(int days, CancellationToken ct = default);

    Task AddAsync(License license, CancellationToken ct = default);
    void Update(License license);

    /// <summary>
    /// Hands the entity to EF as Deleted. The <c>SoftDeleteInterceptor</c>
    /// converts this into a soft-delete at <c>SaveChanges</c> time.
    /// Caller must invoke <c>SaveChangesAsync</c>.
    /// </summary>
    void Remove(License license);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
