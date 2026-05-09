using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.ViewModels.Providers;

/// <summary>
/// Flat row for the provider listing grid. Pre-computes license counts so
/// the view stays dumb and so we don't ship N+1 queries to production.
/// </summary>
public class ProviderListItemVm
{
    public int ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public ProviderStatus Status { get; init; }

    public int TotalLicenseCount { get; init; }
    public int ActiveLicenseCount { get; init; }
    public int ExpiredLicenseCount { get; init; }

    public DateTime CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }

    // Audit-only fields - populated by the Deleted listing.
    public bool IsDeleted { get; init; }
    public DateTime? DeletedDate { get; init; }
    public string? DeletedBy { get; init; }
}
