using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Licenses;

namespace ProviderAssignmentStarter.ViewModels.Providers;

/// <summary>
/// Read-only projection used by the Provider Details and Delete-
/// confirmation pages, and (with audit fields populated) the
/// AuditDetails page.
/// </summary>
/// <remarks>
/// Includes a list of <see cref="LicenseListItemVm"/> children so the
/// Details page renders without a second round-trip. The list is
/// pre-sorted by expiration date (most-imminent first) by the mapper.
/// </remarks>
public class ProviderDetailsVm
{
    public int ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public ProviderStatus Status { get; init; }

    public DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }

    /// <summary>True only when surfaced via the audit page.</summary>
    public bool IsDeleted { get; init; }
    public DateTime? DeletedDate { get; init; }
    public string? DeletedBy { get; init; }

    /// <summary>Licenses for this provider, ordered by expiration ascending.</summary>
    public IReadOnlyList<LicenseListItemVm> Licenses { get; init; } = Array.Empty<LicenseListItemVm>();
}
