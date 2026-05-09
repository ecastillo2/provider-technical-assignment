using ProviderAssignmentStarter.Domain.Common;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Domain.Entities;

/// <summary>
/// An organization or individual that provides services and is managed by
/// state staff. A Provider may have zero or more Licenses.
///
/// Soft-delete contract:
///  - When a Provider is deleted, the SoftDeleteInterceptor flips IsDeleted
///    to true and cascades the same flip to every License under it.
///  - Standard reads are filtered by AppDbContext's global query filter.
///  - Audit reads must explicitly call .IgnoreQueryFilters().
/// </summary>
public class Provider : BaseEntity
{
    public int ProviderId { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public string County { get; set; } = string.Empty;

    public ProviderStatus Status { get; set; } = ProviderStatus.Pending;

    // Navigation
    public ICollection<License> Licenses { get; set; } = new List<License>();
}
