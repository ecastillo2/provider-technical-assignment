using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Licenses;

namespace ProviderAssignmentStarter.ViewModels.Providers;

/// <summary>
/// Read-only projection for the Details / Delete-confirmation views.
/// </summary>
public class ProviderDetailsVm
{
    public int ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public ProviderStatus Status { get; init; }

    public DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }

    public bool IsDeleted { get; init; }
    public DateTime? DeletedDate { get; init; }
    public string? DeletedBy { get; init; }

    public IReadOnlyList<LicenseListItemVm> Licenses { get; init; } = Array.Empty<LicenseListItemVm>();
}
