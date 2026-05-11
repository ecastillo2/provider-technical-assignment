namespace ProviderAssignmentStarter.ViewModels.Paging;

/// <summary>
/// A single page of results plus the metadata a pager UI needs.
/// </summary>
/// <remarks>
/// The repository computes <see cref="TotalCount"/> via <c>CountAsync</c>
/// (one round-trip against the same filtered query) and materialises
/// <see cref="Items"/> via <c>Skip(...).Take(...).ToListAsync(ct)</c> so
/// the database only ships the requested slice. Both calls run against
/// the same composed <c>IQueryable</c>; we never pull-and-discard rows.
/// </remarks>
public sealed class PagedResult<T> : IPagedResultMetadata
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items      = items;
        Page       = page;
        PageSize   = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items      { get; }
    public int              Page       { get; }
    public int              PageSize   { get; }
    public int              TotalCount { get; }

    public int  TotalPages  => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext     => Page < TotalPages;

    /// <summary>Index (1-based) of the first item on this page; 0 when empty.</summary>
    public int FirstItemNumber => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    /// <summary>Index (1-based) of the last item on this page; 0 when empty.</summary>
    public int LastItemNumber  => TotalCount == 0 ? 0 : Math.Min(Page * PageSize, TotalCount);

    /// <summary>Projects to a different item type while preserving paging metadata.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}

/// <summary>Defaults & guards for pagination inputs coming off the querystring.</summary>
public static class Paging
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize     = 100;

    /// <summary>Clamps a user-supplied page number to <c>&gt;= 1</c>.</summary>
    public static int NormalizePage(int? page) => page is null or < 1 ? 1 : page.Value;

    /// <summary>Clamps a user-supplied page size to <c>[1, MaxPageSize]</c>.</summary>
    public static int NormalizePageSize(int? pageSize) =>
        pageSize switch
        {
            null or < 1     => DefaultPageSize,
            > MaxPageSize   => MaxPageSize,
            _               => pageSize.Value,
        };
}
