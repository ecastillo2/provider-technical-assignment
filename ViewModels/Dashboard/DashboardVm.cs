using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Licenses;
using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.ViewModels.Dashboard;

/// <summary>
/// Read-only aggregate model rendered by the Dashboard view. Every metric
/// here is computed by the backend (DashboardService) - the view only
/// renders. The assignment requires that business logic does not live
/// only in the UI.
/// </summary>
public class DashboardVm
{
    public int TotalProviders { get; init; }
    public int TotalLicenses { get; init; }
    public int ActiveProviderCount { get; init; }
    public int InactiveProviderCount { get; init; }
    public int PendingProviderCount { get; init; }

    public int ActiveLicenseCount { get; init; }
    public int ExpiredLicenseCount { get; init; }
    public int SuspendedLicenseCount { get; init; }

    /// <summary>Active providers whose licenses are all expired (status or date).</summary>
    public int ActiveProvidersWithExpiredLicensesCount { get; init; }

    public IReadOnlyDictionary<ProviderStatus, int> ProvidersByStatus { get; init; }
        = new Dictionary<ProviderStatus, int>();

    public IReadOnlyList<LicenseListItemVm> ExpiringSoon { get; init; }
        = Array.Empty<LicenseListItemVm>();

    public IReadOnlyList<ProviderListItemVm> ActiveProvidersWithExpiredLicenses { get; init; }
        = Array.Empty<ProviderListItemVm>();
}
