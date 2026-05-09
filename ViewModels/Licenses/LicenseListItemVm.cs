using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.ViewModels.Licenses;

public class LicenseListItemVm
{
    public int LicenseId { get; init; }
    public int ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;

    public string LicenseNumber { get; init; } = string.Empty;
    public LicenseStatus LicenseStatus { get; init; }
    public DateTime ExpirationDate { get; init; }

    /// <summary>True when status is Active AND expiration not in the past.</summary>
    public bool IsCurrentlyValid => LicenseStatus == LicenseStatus.Active
                                    && ExpirationDate.Date >= DateTime.UtcNow.Date;

    /// <summary>Days until expiration. Negative when already expired.</summary>
    public int DaysUntilExpiration =>
        (int)(ExpirationDate.Date - DateTime.UtcNow.Date).TotalDays;
}
