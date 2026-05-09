using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.ViewModels.Providers;

/// <summary>
/// Flat row used by the Providers listing grid and the Audit (Deleted)
/// listing.
/// </summary>
/// <remarks>
/// <para>
/// License counts are pre-computed by the service / mapper so the Razor
/// view never has to walk a navigation collection. This keeps the view
/// dumb and avoids accidental N+1 queries triggered by lazy loading
/// during render.
/// </para>
/// <para>
/// The audit-only fields (<see cref="IsDeleted"/>, <see cref="DeletedDate"/>,
/// <see cref="DeletedBy"/>) are populated for both standard and audit
/// listings so the same VM can be reused. They are simply blank/false
/// for non-deleted rows.
/// </para>
/// </remarks>
public class ProviderListItemVm
{
    public int ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public ProviderStatus Status { get; init; }

    /// <summary>Total Licenses (excluding soft-deleted) attached to this provider.</summary>
    public int TotalLicenseCount { get; init; }

    /// <summary>Licenses whose status is Active AND whose expiration is not in the past.</summary>
    public int ActiveLicenseCount { get; init; }

    /// <summary>Licenses whose status is Expired OR whose expiration date has passed.</summary>
    public int ExpiredLicenseCount { get; init; }

    public DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }

    // -- Audit-only fields (populated by the Deleted listing) --
    public bool IsDeleted { get; init; }
    public DateTime? DeletedDate { get; init; }
    public string? DeletedBy { get; init; }
}
