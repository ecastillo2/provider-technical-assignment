using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Infrastructure.Data;

namespace ProviderAssignmentStarter.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILicenseRepository"/>.
/// </summary>
public class LicenseRepository : ILicenseRepository
{
    private readonly AppDbContext _db;

    public LicenseRepository(AppDbContext db) => _db = db;

    /// <summary>
    /// Single license by id, with its parent Provider eager-loaded so
    /// the Edit / Delete views can display the provider name without
    /// a second round-trip.
    /// </summary>
    public Task<License?> GetByIdAsync(int licenseId, CancellationToken ct = default) =>
        _db.Licenses
            .Include(l => l.Provider)
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId, ct);

    public async Task<IReadOnlyList<License>> GetByProviderAsync(int providerId, CancellationToken ct = default) =>
        await _db.Licenses
            .AsNoTracking()
            .Where(l => l.ProviderId == providerId)
            .OrderBy(l => l.ExpirationDate) // most-imminent expirations first
            .ToListAsync(ct);

    public async Task<IReadOnlyList<License>> GetExpiringSoonAsync(int days, CancellationToken ct = default)
    {
        // Closed interval [today, today+days]. We deliberately match on
        // .Date precision so a same-day expiration counts as "expiring".
        var today  = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(days);

        return await _db.Licenses
            .AsNoTracking()
            .Include(l => l.Provider) // Provider name is rendered on the dashboard row
            .Where(l => l.LicenseStatus == LicenseStatus.Active
                        && l.ExpirationDate >= today
                        && l.ExpirationDate <= cutoff)
            .OrderBy(l => l.ExpirationDate)
            .ToListAsync(ct);
    }

    public async Task AddAsync(License license, CancellationToken ct = default) =>
        await _db.Licenses.AddAsync(license, ct);

    public void Update(License license) => _db.Licenses.Update(license);

    public void Remove(License license) => _db.Licenses.Remove(license); // → soft-delete via interceptor

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
