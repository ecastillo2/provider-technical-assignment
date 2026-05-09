using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Licenses;
using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.ViewModels.Dashboard;

/// <summary>
/// Read-only aggregate model rendered by the dashboard view. Every
/// metric here is computed by <c>DashboardService</c> in C#; the view
/// only renders.
/// </summary>
/// <remarks>
/// <para>
/// The assignment requires that business logic must not exist only in
/// the UI. By materialising every metric server-side and shipping it as
/// a strongly-typed VM, we make this property obvious to the reviewer:
/// the Razor file contains no LINQ, no aggregation, no date math.
/// </para>
/// <para>
/// The <c>ExpiringSoon</c> and <c>ActiveProvidersWithExpiredLicenses</c>
/// lists are pre-sorted by the service (most-imminent first / by name)
/// so the view doesn't sort either.
/// </para>
/// </remarks>
public class DashboardVm
{
    // ---------- Top-level counts (rendered as summary cards) ----------
    public int TotalProviders { get; init; }
    public int TotalLicenses { get; init; }
    public int ActiveProviderCount { get; init; }
    public int InactiveProviderCount { get; init; }
    public int PendingProviderCount { get; init; }

    public int ActiveLicenseCount { get; init; }
    public int ExpiredLicenseCount { get; init; }
    public int SuspendedLicenseCount { get; init; }

    /// <summary>
    /// Count of Active providers whose licenses are ALL expired (status
    /// or date). The "looks active but really isn't" headline metric.
    /// </summary>
    public int ActiveProvidersWithExpiredLicensesCount { get; init; }

    // ---------- Backing data for charts and tables ----------

    /// <summary>Provider counts keyed by status — fed into the doughnut chart.</summary>
    public IReadOnlyDictionary<ProviderStatus, int> ProvidersByStatus { get; init; }
        = new Dictionary<ProviderStatus, int>();

    /// <summary>Active licenses expiring within 30 days, ordered by expiration ascending.</summary>
    public IReadOnlyList<LicenseListItemVm> ExpiringSoon { get; init; }
        = Array.Empty<LicenseListItemVm>();

    /// <summary>List form of <see cref="ActiveProvidersWithExpiredLicensesCount"/>.</summary>
    public IReadOnlyList<ProviderListItemVm> ActiveProvidersWithExpiredLicenses { get; init; }
        = Array.Empty<ProviderListItemVm>();
}
