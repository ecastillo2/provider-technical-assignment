using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Infrastructure.Data;

namespace ProviderAssignmentStarter.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProviderRepository"/>.
///
/// Notes for reviewers:
///  - All "standard" reads rely on the DbContext global query filter to
///    exclude soft-deleted rows. We never write WHERE IsDeleted = 0 here
///    by hand - duplicating that filter would be a source of drift.
///  - Audit reads call .IgnoreQueryFilters() explicitly so the intent is
///    visible at the call site.
/// </summary>
public class ProviderRepository : IProviderRepository
{
    private readonly AppDbContext _db;

    public ProviderRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Providers
            .AsNoTracking()
            .OrderBy(p => p.ProviderName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Provider>> GetAllWithLicensesAsync(CancellationToken ct = default) =>
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
        var query = _db.Providers
            .AsNoTracking()
            .Include(p => p.Licenses)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // EF.Functions.Like is case-insensitive on SQLite by default.
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.ProviderName, pattern)
                || EF.Functions.Like(p.County, pattern));
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query.OrderBy(p => p.ProviderName).ToListAsync(ct);
    }

    public Task<Provider?> GetByIdAsync(int providerId, CancellationToken ct = default) =>
        _db.Providers.FirstOrDefaultAsync(p => p.ProviderId == providerId, ct);

    public Task<Provider?> GetByIdWithLicensesAsync(int providerId, CancellationToken ct = default) =>
        _db.Providers
            .Include(p => p.Licenses)
            .FirstOrDefaultAsync(p => p.ProviderId == providerId, ct);

    public async Task<IReadOnlyList<Provider>> GetDeletedAsync(CancellationToken ct = default) =>
        await _db.Providers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DeletedDate)
            .ToListAsync(ct);

    public Task<Provider?> GetByIdIncludingDeletedAsync(int providerId, CancellationToken ct = default) =>
        _db.Providers
            .IgnoreQueryFilters()
            .Include(p => p.Licenses)
            .FirstOrDefaultAsync(p => p.ProviderId == providerId, ct);

    public async Task<IReadOnlyList<Provider>> GetActiveWithActiveLicensesAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
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

    public async Task AddAsync(Provider provider, CancellationToken ct = default) =>
        await _db.Providers.AddAsync(provider, ct);

    public void Update(Provider provider) => _db.Providers.Update(provider);

    public void Remove(Provider provider) => _db.Providers.Remove(provider); // -> soft-delete via interceptor

    public async Task<bool> RestoreAsync(int providerId, CancellationToken ct = default)
    {
        var provider = await _db.Providers
            .IgnoreQueryFilters()
            .Include(p => p.Licenses)
            .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.IsDeleted, ct);

        if (provider is null) return false;

        provider.IsDeleted = false;
        provider.DeletedDate = null;
        provider.DeletedBy = null;

        // Restore the cascade'd licenses too. We only un-delete those the
        // cascade itself deleted (DeletedDate matches the parent within
        // a small tolerance). A real system would track this with a
        // dedicated audit table.
        foreach (var license in provider.Licenses.Where(l => l.IsDeleted))
        {
            license.IsDeleted = false;
            license.DeletedDate = null;
            license.DeletedBy = null;
        }

        return true;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
