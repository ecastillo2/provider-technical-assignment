using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.ViewModels.Paging;

namespace ProviderAssignmentStarter.Infrastructure.Repositories;

/// <summary>
/// Persistence-agnostic contract for the <see cref="Provider"/> aggregate.
/// </summary>
/// <remarks>
/// <para>
/// Letting the service layer depend on this interface (rather than on
/// <c>DbContext</c> directly) buys us:
/// <list type="bullet">
///   <item><b>Dependency Inversion</b> — services know nothing about EF.</item>
///   <item><b>Testability</b> — substitute an in-memory fake for unit tests; production tests use the real implementation against SQLite in-memory.</item>
///   <item><b>A clear surface for the audit escape hatch</b> — every method whose name contains <c>Deleted</c> opts out of the global query filter.</item>
/// </list>
/// </para>
/// <para>
/// Methods are split by intent:
/// <list type="bullet">
///   <item><b>Standard reads</b> (<c>GetAllAsync</c>, <c>GetByIdAsync</c>, etc.) — go through the global query filter; never see soft-deleted rows.</item>
///   <item><b>Audit reads</b> (<c>GetDeletedAsync</c>, <c>GetByIdIncludingDeletedAsync</c>) — explicitly call <c>.IgnoreQueryFilters()</c>.</item>
///   <item><b>Mutation</b> (<c>AddAsync</c>, <c>Update</c>, <c>Remove</c>, <c>RestoreAsync</c>) — all eventually go through the soft-delete interceptor.</item>
/// </list>
/// </para>
/// </remarks>
public interface IProviderRepository
{
    /// <summary>All non-deleted providers, ordered by name. No licenses.</summary>
    Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Non-deleted providers with their licenses eager-loaded in a single
    /// round-trip. Used by the listing grid so it can show counts without
    /// N+1 queries. Licenses on each provider are also filtered (the
    /// global query filter applies through Include).
    /// </summary>
    Task<IReadOnlyList<Provider>> GetAllWithLicensesAsync(CancellationToken ct = default);

    /// <summary>
    /// Filtered listing — free-text against name/county and an optional
    /// status restriction. Filter values are pushed into SQL via
    /// <c>EF.Functions.Like</c> so we never pull-and-discard.
    /// </summary>
    Task<IReadOnlyList<Provider>> SearchAsync(
        string? search,
        Domain.Enums.ProviderStatus? status,
        CancellationToken ct = default);

    /// <summary>
    /// Paged variant of <see cref="SearchAsync"/>. Composes the same
    /// IQueryable, runs <c>CountAsync</c> against it for the total, then
    /// returns only the requested slice via <c>Skip/Take</c>. Both round-
    /// trips honour the global query filter.
    /// </summary>
    Task<PagedResult<Provider>> SearchPagedAsync(
        string? search,
        Domain.Enums.ProviderStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Single provider by id, or null if missing or soft-deleted.</summary>
    Task<Provider?> GetByIdAsync(int providerId, CancellationToken ct = default);

    /// <summary>
    /// Single provider with its Licenses eager-loaded. Returns null if
    /// the provider is missing or soft-deleted. Used by the Details page
    /// and by <c>SoftDeleteAsync</c> (so the interceptor's tracked-cascade
    /// path applies).
    /// </summary>
    Task<Provider?> GetByIdWithLicensesAsync(int providerId, CancellationToken ct = default);

    // ---------------------------------------------------------------
    // Audit / admin escape hatches. These are the ONLY methods that
    // call .IgnoreQueryFilters(). The grep-able naming convention
    // makes the intent visible at every call site.
    // ---------------------------------------------------------------

    /// <summary>Audit listing: soft-deleted providers only, newest first.</summary>
    Task<IReadOnlyList<Provider>> GetDeletedAsync(CancellationToken ct = default);

    /// <summary>Paged variant of <see cref="GetDeletedAsync"/>.</summary>
    Task<PagedResult<Provider>> GetDeletedPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Streams every non-deleted provider as the database produces it,
    /// without materialising the whole result into a <c>List&lt;T&gt;</c>.
    /// Use for bulk exports / large reads where the caller does not need
    /// the entire collection at once.
    /// </summary>
    IAsyncEnumerable<Provider> StreamAllAsync(CancellationToken ct = default);

    /// <summary>Audit fetch: any provider (deleted or not), with licenses.</summary>
    Task<Provider?> GetByIdIncludingDeletedAsync(int providerId, CancellationToken ct = default);

    /// <summary>
    /// Active providers paired with their currently-valid licenses.
    /// Backed by the same predicate as <c>vw_ActiveProvidersWithActiveLicenses</c>.
    /// </summary>
    Task<IReadOnlyList<Provider>> GetActiveWithActiveLicensesAsync(CancellationToken ct = default);

    /// <summary>
    /// Active providers whose licenses are ALL expired (status or date).
    /// Surfaces the assignment's flagship "looks active but really isn't"
    /// scenario. Backed by the same predicate as
    /// <c>vw_ActiveProvidersWithExpiredLicenses</c>.
    /// </summary>
    Task<IReadOnlyList<Provider>> GetActiveWithExpiredLicensesAsync(CancellationToken ct = default);

    // ---------------------------------------------------------------
    // Mutation. SaveChangesAsync is exposed so the service can batch
    // multiple repository calls into a single transaction.
    // ---------------------------------------------------------------

    Task AddAsync(Provider provider, CancellationToken ct = default);

    void Update(Provider provider);

    /// <summary>
    /// Marks the provider for deletion. The <c>SoftDeleteInterceptor</c>
    /// converts this into a soft-delete at <c>SaveChanges</c> time and
    /// cascades to the provider's Licenses.
    /// </summary>
    void Remove(Provider provider);

    /// <summary>
    /// Reverses a soft-delete. Returns false if the provider is not
    /// currently soft-deleted (caller can return 404). Cascades the
    /// restore to any Licenses that were soft-deleted as part of the
    /// original cascade.
    /// </summary>
    Task<bool> RestoreAsync(int providerId, CancellationToken ct = default);

    /// <summary>Persists pending changes. Returns the affected row count.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
