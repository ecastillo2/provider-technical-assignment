namespace ProviderAssignmentStarter.ViewModels.Paging;

/// <summary>
/// View-side adapter for the <c>_Pager</c> partial. Bundles the page
/// metadata together with the route-value dictionary the pager needs to
/// regenerate links while preserving filter state (search, status, etc.).
/// </summary>
public sealed class PagerModel
{
    public PagerModel(IPagedResultMetadata page, IDictionary<string, object?> route)
    {
        Page  = page;
        Route = route;
    }

    public IPagedResultMetadata          Page  { get; }
    public IDictionary<string, object?>  Route { get; }

    /// <summary>
    /// Returns a copy of <see cref="Route"/> with <c>page</c> overwritten
    /// to <paramref name="pageNumber"/>. Used by the pager to build the
    /// href for each numbered link without mutating shared state.
    /// </summary>
    public IDictionary<string, object?> RouteWithPage(int pageNumber)
    {
        var clone = new Dictionary<string, object?>(Route, StringComparer.OrdinalIgnoreCase)
        {
            ["page"] = pageNumber,
        };
        return clone;
    }
}

/// <summary>
/// Minimal contract <see cref="PagerModel"/> needs from a paged result.
/// Lets the partial render against any concrete <c>PagedResult&lt;T&gt;</c>
/// without taking a generic type argument.
/// </summary>
public interface IPagedResultMetadata
{
    int  Page            { get; }
    int  PageSize        { get; }
    int  TotalCount      { get; }
    int  TotalPages      { get; }
    bool HasPrevious     { get; }
    bool HasNext         { get; }
    int  FirstItemNumber { get; }
    int  LastItemNumber  { get; }
}
