using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Infrastructure.Data;
using ProviderAssignmentStarter.Infrastructure.Repositories;
using ProviderAssignmentStarter.Services.Mapping;
using ProviderAssignmentStarter.ViewModels.Dashboard;

namespace ProviderAssignmentStarter.Services;

/// <summary>
/// Computes every dashboard metric in the backend so business rules don't
/// leak into the view (an explicit assignment requirement).
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext        _db;
    private readonly IProviderRepository _providers;
    private readonly ILicenseRepository  _licenses;

    public DashboardService(AppDbContext db, IProviderRepository providers, ILicenseRepository licenses)
    {
        _db        = db;
        _providers = providers;
        _licenses  = licenses;
    }

    public async Task<DashboardVm> BuildAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        // Counts run as a single round-trip set of small queries.
        var totalProviders = await _db.Providers.CountAsync(ct);
        var totalLicenses  = await _db.Licenses.CountAsync(ct);

        var providerStatusGroups = await _db.Providers
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var providersByStatus = providerStatusGroups
            .ToDictionary(x => x.Status, x => x.Count);

        var activeLicenseCount    = await _db.Licenses
            .CountAsync(l => l.LicenseStatus == LicenseStatus.Active && l.ExpirationDate >= today, ct);
        var expiredLicenseCount   = await _db.Licenses
            .CountAsync(l => l.LicenseStatus == LicenseStatus.Expired || l.ExpirationDate < today, ct);
        var suspendedLicenseCount = await _db.Licenses
            .CountAsync(l => l.LicenseStatus == LicenseStatus.Suspended, ct);

        var activeProvidersWithExpired = await _providers.GetActiveWithExpiredLicensesAsync(ct);
        var expiringSoonLicenses       = await _licenses.GetExpiringSoonAsync(30, ct);

        return new DashboardVm
        {
            TotalProviders                          = totalProviders,
            TotalLicenses                           = totalLicenses,
            ActiveProviderCount                     = providersByStatus.GetValueOrDefault(ProviderStatus.Active),
            InactiveProviderCount                   = providersByStatus.GetValueOrDefault(ProviderStatus.Inactive),
            PendingProviderCount                    = providersByStatus.GetValueOrDefault(ProviderStatus.Pending),
            ActiveLicenseCount                      = activeLicenseCount,
            ExpiredLicenseCount                     = expiredLicenseCount,
            SuspendedLicenseCount                   = suspendedLicenseCount,
            ActiveProvidersWithExpiredLicensesCount = activeProvidersWithExpired.Count,
            ProvidersByStatus                       = providersByStatus,
            ExpiringSoon                            = expiringSoonLicenses
                .Select(l => l.ToListItem(l.Provider?.ProviderName ?? string.Empty)).ToList(),
            ActiveProvidersWithExpiredLicenses      = activeProvidersWithExpired
                .Select(p => p.ToListItem()).ToList()
        };
    }
}
