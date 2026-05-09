using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Infrastructure.Data;

namespace ProviderAssignmentStarter.Infrastructure.Repositories;

public class LicenseRepository : ILicenseRepository
{
    private readonly AppDbContext _db;

    public LicenseRepository(AppDbContext db) => _db = db;

    public Task<License?> GetByIdAsync(int licenseId, CancellationToken ct = default) =>
        _db.Licenses
            .Include(l => l.Provider)
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId, ct);

    public async Task<IReadOnlyList<License>> GetByProviderAsync(int providerId, CancellationToken ct = default) =>
        await _db.Licenses
            .AsNoTracking()
            .Where(l => l.ProviderId == providerId)
            .OrderBy(l => l.ExpirationDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<License>> GetExpiringSoonAsync(int days, CancellationToken ct = default)
    {
        var today  = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(days);
        return await _db.Licenses
            .AsNoTracking()
            .Include(l => l.Provider)
            .Where(l => l.LicenseStatus == LicenseStatus.Active
                        && l.ExpirationDate >= today
                        && l.ExpirationDate <= cutoff)
            .OrderBy(l => l.ExpirationDate)
            .ToListAsync(ct);
    }

    public async Task AddAsync(License license, CancellationToken ct = default) =>
        await _db.Licenses.AddAsync(license, ct);

    public void Update(License license) => _db.Licenses.Update(license);

    public void Remove(License license) => _db.Licenses.Remove(license);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
