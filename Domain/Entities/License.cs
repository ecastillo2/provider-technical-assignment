using ProviderAssignmentStarter.Domain.Common;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Domain.Entities;

/// <summary>
/// A license issued to a Provider authorising operations for a defined
/// period. Validity is determined by both LicenseStatus AND ExpirationDate.
/// </summary>
public class License : BaseEntity
{
    public int LicenseId { get; set; }

    public int ProviderId { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;

    public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.Active;

    public DateTime ExpirationDate { get; set; }

    // Navigation
    public Provider Provider { get; set; } = null!;

    /// <summary>
    /// True when status is Active AND the expiration date has not passed.
    /// Computed in domain rather than persisted so it can never go stale.
    /// </summary>
    public bool IsCurrentlyValid =>
        LicenseStatus == LicenseStatus.Active && ExpirationDate.Date >= DateTime.UtcNow.Date;
}
