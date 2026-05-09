using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.ViewModels.Licenses;

/// <summary>
/// Read-only row used by the License lists on the Provider Details page,
/// the Delete confirmation page, and the dashboard's "expiring soon"
/// table.
/// </summary>
/// <remarks>
/// The two computed properties at the bottom are intentionally NOT
/// persisted — they depend on the wall clock and would go stale.
/// </remarks>
public class LicenseListItemVm
{
    public int LicenseId { get; init; }
    public int ProviderId { get; init; }

    /// <summary>The parent provider's display name. Pre-resolved by the mapper.</summary>
    public string ProviderName { get; init; } = string.Empty;

    public string LicenseNumber { get; init; } = string.Empty;
    public LicenseStatus LicenseStatus { get; init; }
    public DateTime ExpirationDate { get; init; }

    /// <summary>
    /// True only when status is Active AND the expiration date has not
    /// yet passed. The single source of truth for "is this license
    /// usable right now".
    /// </summary>
    public bool IsCurrentlyValid => LicenseStatus == LicenseStatus.Active
                                    && ExpirationDate.Date >= DateTime.UtcNow.Date;

    /// <summary>
    /// Days until expiration (negative when already expired). Used by
    /// the dashboard's "expiring soon" widget to render the countdown.
    /// </summary>
    public int DaysUntilExpiration =>
        (int)(ExpirationDate.Date - DateTime.UtcNow.Date).TotalDays;
}
