using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Infrastructure.Data;
using ProviderAssignmentStarter.Infrastructure.Repositories;
using ProviderAssignmentStarter.Services.Mapping;
using ProviderAssignmentStarter.ViewModels.Dashboard;

namespace ProviderAssignmentStarter.Services;

/// <summary>
/// Default implementation of <see cref="IDashboardService"/>. Computes
/// every metric in C# against the repositories and the DbContext.
/// </summary>
/// <remarks>
/// <para>
/// This service is the only place outside the repositories that touches
/// the DbContext directly. The reason: the dashboard issues several small
/// COUNT queries that don't naturally belong on either repository
/// interface. Pulling them down here keeps the repos focused on entity
/// CRUD while still avoiding leaking EF into the controller.
/// </para>
/// <para>
/// In a higher-traffic system this would be replaced with a single CTE
/// that computes all metrics in one round-trip. For the seeded volume
/// (handful of providers, dozen licenses) the round-trip count is
/// irrelevant. Documented as a future improvement in the README.
/// </para>
/// </remarks>
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
        // Every COUNT here flows through the global query filter, so
        // soft-deleted rows are automatically excluded from every metric.
        var today = DateTime.UtcNow.Date;

        // ---------- Top-level totals ----------
        var totalProviders = await _db.Providers.CountAsync(ct);
        var totalLicenses  = await _db.Licenses.CountAsync(ct);

        // ---------- Providers grouped by status ----------
        // Materialise to a dictionary so we can render in any order on
        // the view without re-querying.
        var providerStatusGroups = await _db.Providers
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var providersByStatus = providerStatusGroups
            .ToDictionary(x => x.Status, x => x.Count);

        // ---------- Licenses grouped by validity ----------
        // We deliberately count "currently valid" rather than "status =
        // Active": a status-Active-but-date-expired license is NOT
        // currently valid and counts in the expired bucket.
        var activeLicenseCount    = await _db.Licenses
            .CountAsync(l => l.LicenseStatus == LicenseStatus.Active && l.ExpirationDate >= today, ct);
        var expiredLicenseCount   = await _db.Licenses
            .CountAsync(l => l.LicenseStatus == LicenseStatus.Expired || l.ExpirationDate < today, ct);
        var suspendedLicenseCount = await _db.Licenses
            .CountAsync(l => l.LicenseStatus == LicenseStatus.Suspended, ct);

        // ---------- Spec-driven cross-cutting widgets ----------
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
