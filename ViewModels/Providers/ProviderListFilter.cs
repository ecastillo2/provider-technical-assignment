using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.ViewModels.Providers;

/// <summary>
/// Bound to the listing-page filter form. Every property is optional;
/// missing properties mean "no filter on that dimension".
///
/// The values flow through the URL querystring so any filtered view
/// is shareable / bookmarkable.
/// </summary>
public class ProviderListFilter
{
    /// <summary>Free-text match against ProviderName or County (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Optional status restriction.</summary>
    public ProviderStatus? Status { get; set; }

    public bool HasFilter =>
        !string.IsNullOrWhiteSpace(Search) || Status.HasValue;
}
