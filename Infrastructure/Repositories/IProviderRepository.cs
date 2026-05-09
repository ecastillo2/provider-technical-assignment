using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Repositories;

/// <summary>
/// Persistence-agnostic contract for the Provider aggregate. Letting the
/// service layer depend on this interface (rather than DbContext directly)
/// gives us the Dependency Inversion principle and makes services unit-
/// testable with an in-memory fake.
/// </summary>
public interface IProviderRepository
{
    /// <summary>Active (non-deleted) providers, ordered by name.</summary>
    Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Active providers with their licenses eager-loaded. Used by the
    /// listing grid so we render in one query instead of N+1.
    /// </summary>
    Task<IReadOnlyList<Provider>> GetAllWithLicensesAsync(CancellationToken ct = default);

    /// <summary>Single provider by id, or null if missing or soft-deleted.</summary>
    Task<Provider?> GetByIdAsync(int providerId, CancellationToken ct = default);

    /// <summary>Single provider including its licenses (eager-loaded).</summary>
    Task<Provider?> GetByIdWithLicensesAsync(int providerId, CancellationToken ct = default);

    /// <summary>Audit escape hatch: soft-deleted providers only.</summary>
    Task<IReadOnlyList<Provider>> GetDeletedAsync(CancellationToken ct = default);

    /// <summary>Audit escape hatch: any provider (deleted or not), with licenses.</summary>
    Task<Provider?> GetByIdIncludingDeletedAsync(int providerId, CancellationToken ct = default);

    /// <summary>
    /// Active providers whose Active+not-expired licenses are returned alongside.
    /// Backed by vw_ActiveProvidersWithActiveLicenses semantics, expressed in LINQ
    /// so it remains testable.
    /// </summary>
    Task<IReadOnlyList<Provider>> GetActiveWithActiveLicensesAsync(CancellationToken ct = default);

    /// <summary>
    /// Providers recorded as Active whose licenses are all expired (status or date).
    /// Surfaces the "looks active but really isn't" scenario from the spec.
    /// </summary>
    Task<IReadOnlyList<Provider>> GetActiveWithExpiredLicensesAsync(CancellationToken ct = default);

    Task AddAsync(Provider provider, CancellationToken ct = default);
    void Update(Provider provider);
    void Remove(Provider provider); // Interceptor converts this to soft-delete.
    Task<bool> RestoreAsync(int providerId, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
