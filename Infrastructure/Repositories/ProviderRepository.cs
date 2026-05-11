using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Infrastructure.Data;
using ProviderAssignmentStarter.ViewModels.Paging;

namespace ProviderAssignmentStarter.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProviderRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// Notes for reviewers:
/// </para>
/// <list type="bullet">
///   <item>
///     <b>No hand-written soft-delete filters.</b> Standard reads rely on
///     the DbContext global query filter to exclude soft-deleted rows.
///     Re-stating <c>WHERE IsDeleted = 0</c> here would create a second
///     source of truth that could drift away from the filter.
///   </item>
///   <item>
///     <b>Audit reads are deliberate.</b> Methods that need to see
///     soft-deleted rows call <c>.IgnoreQueryFilters()</c> explicitly
///     so the intent is visible at the call site.
///   </item>
///   <item>
///     <b>Read paths use <c>AsNoTracking</c></b> to avoid the change
///     tracker overhead. Mutation paths track normally.
///   </item>
/// </list>
/// </remarks>
public class ProviderRepository : IProviderRepository
{
    private readonly AppDbContext _db;

    public ProviderRepository(AppDbContext db) => _db = db;

    // ============================================================
    //  Standard reads (global query filter active)
    // ============================================================

    public async Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Providers
            .AsNoTracking()
            .OrderBy(p => p.ProviderName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Provider>> GetAllWithLicensesAsync(CancellationToken ct = default) =>
        // Single round-trip with Include — avoids N+1 on the listing.
        await _db.Providers
            .AsNoTracking()
            .Include(p => p.Licenses)
            .OrderBy(p => p.ProviderName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Provider>> SearchAsync(
        string? search,
        ProviderStatus? status,
        CancellationToken ct = default)
    {
        var query = BuildSearchQuery(search, status);
        return await query.OrderBy(p => p.ProviderName).ToListAsync(ct);
    }

    public async Task<PagedResult<Provider>> SearchPagedAsync(
        string? search,
        ProviderStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = BuildSearchQuery(search, status).OrderBy(p => p.ProviderName);

        // Two round-trips against the SAME composed IQueryable:
        //   1) CountAsync materialises a SELECT COUNT(*) — no rows shipped.
        //   2) Skip/Take/ToListAsync materialises just the requested slice.
        // The database does the heavy lifting; we never pull-and-discard.
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Provider>(items, page, pageSize, total);
    }

    /// <summary>
    /// Shared composition for <see cref="SearchAsync"/> and
    /// <see cref="SearchPagedAsync"/>. Returns an <c>IQueryable</c> so the
    /// caller can decide whether to materialise, page, or stream.
    /// </summary>
    private IQueryable<Provider> BuildSearchQuery(string? search, ProviderStatus? status)
    {
        // Build the query progressively. Each filter is only applied
        // when it has a value, so an empty filter falls through to the
        // same query GetAllWithLicensesAsync would emit.
        var query = _db.Providers
            .AsNoTracking()
            .Include(p => p.Licenses)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // EF.Functions.Like translates to a SQL LIKE, which on
            // SQLite is case-insensitive for ASCII characters by default
            // — no need for explicit lower(). The pattern is wrapped in
            // %s on both sides for substring match.
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.ProviderName, pattern)
                || EF.Functions.Like(p.County, pattern));
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return query;
    }

    /// <summary>
    /// Streaming read: rows flow from EF as the underlying data reader
    /// produces them. The caller iterates with <c>await foreach</c> and
    /// nothing is buffered into a <c>List&lt;T&gt;</c>.
    /// </summary>
    public IAsyncEnumerable<Provider> StreamAllAsync(CancellationToken ct = default) =>
        _db.Providers
            .AsNoTracking()
            .Include(p => p.Licenses)
            .OrderBy(p => p.ProviderName)
            .AsAsyncEnumerable();

    public Task<Provider?> GetByIdAsync(int providerId, CancellationToken ct = default) =>
        // Tracked, no Include — used for Update and "lightweight" reads.
        _db.Providers.FirstOrDefaultAsync(p => p.ProviderId == providerId, ct);

    public Task<Provider?> GetByIdWithLicensesAsync(int providerId, CancellationToken ct = default) =>
        // Tracked + Include. Used by Details (read) and SoftDelete (write).
        _db.Providers
            .Include(p => p.Licenses)
            .FirstOrDefaultAsync(p => p.ProviderId == providerId, ct);

    // ============================================================
    //  Audit / admin escape hatches (filter ignored)
    // ============================================================

    public async Task<IReadOnlyList<Provider>> GetDeletedAsync(CancellationToken ct = default) =>
        await BuildDeletedQuery().ToListAsync(ct);

    public async Task<PagedResult<Provider>> GetDeletedPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = BuildDeletedQuery();
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Provider>(items, page, pageSize, total);
    }

    /// <summary>
    /// Shared composition for the audit listing. Eager-loads Licenses so
    /// the list-item VM can render true license counts (without this
    /// Include, .Licenses materialises as empty under no-lazy-loading and
    /// the counts come out as 0).
    /// </summary>
    private IQueryable<Provider> BuildDeletedQuery() =>
        _db.Providers
            .IgnoreQueryFilters()              // see soft-deleted rows
            .AsNoTracking()
            .Include(p => p.Licenses)          // counts accurate on the audit grid
            .Where(p => p.IsDeleted)           // and ONLY soft-deleted rows
            .OrderByDescending(p => p.DeletedDate);

    public Task<Provider?> GetByIdIncludingDeletedAsync(int providerId, CancellationToken ct = default) =>
        _db.Providers
            .IgnoreQueryFilters()
            .Include(p => p.Licenses)     // child filter is ALSO bypassed by IgnoreQueryFilters
            .FirstOrDefaultAsync(p => p.ProviderId == providerId, ct);

    // ============================================================
    //  Required-scenario queries (filter active)
    // ============================================================

    public async Task<IReadOnlyList<Provider>> GetActiveWithActiveLicensesAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        // Project Provider with a SUBSET of its Licenses (only those that
        // are currently valid). The .Where(p => p.Licenses.Any()) at the
        // end drops providers that have no valid licenses to surface.
        return await _db.Providers
            .AsNoTracking()
            .Where(p => p.Status == ProviderStatus.Active)
            .Select(p => new Provider
            {
                ProviderId   = p.ProviderId,
                ProviderName = p.ProviderName,
                County       = p.County,
                Status       = p.Status,
                CreatedDate  = p.CreatedDate,
                Licenses = p.Licenses
                    .Where(l => l.LicenseStatus == LicenseStatus.Active && l.ExpirationDate >= today)
                    .ToList()
            })
            .Where(p => p.Licenses.Any())
            .OrderBy(p => p.ProviderName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Provider>> GetActiveWithExpiredLicensesAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        // The flagship "looks active but really isn't" scenario:
        //   - Provider.Status is Active
        //   - The provider has at least one License
        //   - EVERY License is either expired-by-status OR expired-by-date
        //
        // The .All() predicate at the database level is critical — we
        // must NOT match a provider that has any currently-valid license.
        return await _db.Providers
            .AsNoTracking()
            .Include(p => p.Licenses)
            .Where(p => p.Status == ProviderStatus.Active
                        && p.Licenses.Any()
                        && p.Licenses.All(l => l.LicenseStatus == LicenseStatus.Expired
                                               || l.ExpirationDate < today))
            .OrderBy(p => p.ProviderName)
            .ToListAsync(ct);
    }

    // ============================================================
    //  Mutation
    // ============================================================

    public async Task AddAsync(Provider provider, CancellationToken ct = default) =>
        await _db.Providers.AddAsync(provider, ct);

    public void Update(Provider provider) => _db.Providers.Update(provider);

    /// <summary>
    /// Hands the entity to EF as Deleted. The <c>SoftDeleteInterceptor</c>
    /// converts this into a soft-delete (and cascades to children) at
    /// <c>SaveChanges</c> time. Caller must invoke <c>SaveChangesAsync</c>.
    /// </summary>
    public void Remove(Provider provider) => _db.Providers.Remove(provider);

    public async Task<bool> RestoreAsync(int providerId, CancellationToken ct = default)
    {
        // Use the audit escape hatch to find the soft-deleted record;
        // a normal query would never see it.
        var provider = await _db.Providers
            .IgnoreQueryFilters()
            .Include(p => p.Licenses)
            .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.IsDeleted, ct);

        if (provider is null) return false;

        // Reset the soft-delete columns on the parent.
        provider.IsDeleted = false;
        provider.DeletedDate = null;
        provider.DeletedBy = null;

        // Reverse the cascade: any Licenses that were soft-deleted along
        // with the parent are restored too. We naively flip every
        // currently-deleted License — a real system would track which
        // ones the original cascade touched (via an AuditLog table) so a
        // license that was deleted independently stays deleted. This is
        // listed in README §11 as a future improvement.
        foreach (var license in provider.Licenses.Where(l => l.IsDeleted))
        {
            license.IsDeleted = false;
            license.DeletedDate = null;
            license.DeletedBy = null;
        }

        return true;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
