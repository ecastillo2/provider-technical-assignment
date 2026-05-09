using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.ViewModels.Providers;

/// <summary>
/// Backing model for the search/filter form on the Providers Index.
/// </summary>
/// <remarks>
/// <para>
/// Bound from the querystring (<c>[FromQuery]</c>) so any filtered URL
/// is shareable / bookmarkable. Every property is optional; absent
/// properties mean "no filter on that dimension", and the controller
/// short-circuits to the unfiltered listing when <see cref="HasFilter"/>
/// is false.
/// </para>
/// <para>
/// Filtering is pushed all the way down to SQL via the repository
/// (<c>EF.Functions.Like</c>); we never pull-and-discard.
/// </para>
/// </remarks>
public class ProviderListFilter
{
    /// <summary>
    /// Free-text match against ProviderName OR County (case-insensitive
    /// for ASCII on SQLite).
    /// </summary>
    public string? Search { get; set; }

    /// <summary>Optional status restriction. Null means "any status".</summary>
    public ProviderStatus? Status { get; set; }

    /// <summary>True iff at least one filter is active.</summary>
    public bool HasFilter =>
        !string.IsNullOrWhiteSpace(Search) || Status.HasValue;
}
