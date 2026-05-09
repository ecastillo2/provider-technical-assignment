using ProviderAssignmentStarter.Domain.Common;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Domain.Entities;

/// <summary>
/// Aggregate root representing an organization or individual that provides
/// services and is managed by state staff.
/// </summary>
/// <remarks>
/// <para>
/// A Provider may have zero or more <see cref="License"/> children. The
/// relationship is parent-mandatory (every License belongs to exactly one
/// Provider) and child-cascading at the soft-delete layer:
/// </para>
/// <list type="bullet">
///   <item>When a Provider is soft-deleted, all of its Licenses are also soft-deleted in the same <c>SaveChanges</c> call (handled by <c>SoftDeleteInterceptor</c>).</item>
///   <item>When a Provider is restored, the cascaded Licenses are restored as well (see <c>ProviderRepository.RestoreAsync</c>).</item>
/// </list>
/// <para>
/// All audit and soft-delete columns are inherited from <see cref="BaseEntity"/>.
/// Schema concerns (column types, indexes, CHECK constraints, FK behaviour)
/// live in <c>ProviderConfiguration</c>, not here — this class stays a pure
/// POCO so the Domain layer remains free of EF references.
/// </para>
/// </remarks>
public class Provider : BaseEntity
{
    /// <summary>Surrogate primary key. Auto-incremented by SQLite.</summary>
    public int ProviderId { get; set; }

    /// <summary>Display name. Indexed for the listing grid.</summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>County in which the provider operates (e.g. Fulton, DeKalb).</summary>
    public string County { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle status — see <see cref="ProviderStatus"/>. Note that an
    /// Active provider does not necessarily have valid licenses.
    /// </summary>
    public ProviderStatus Status { get; set; } = ProviderStatus.Pending;

    /// <summary>
    /// Children of this Provider. Hydrated via <c>Include(p =&gt; p.Licenses)</c>
    /// when the call site needs them; otherwise empty.
    /// </summary>
    public ICollection<License> Licenses { get; set; } = new List<License>();
}
