using ProviderAssignmentStarter.Domain.Common;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Domain.Entities;

/// <summary>
/// A license issued to a <see cref="Provider"/> authorising operations
/// for a defined period.
/// </summary>
/// <remarks>
/// <para>
/// Validity is determined by BOTH the <see cref="LicenseStatus"/> AND
/// <see cref="ExpirationDate"/>. A license whose status is
/// <see cref="Domain.Enums.LicenseStatus.Active"/> but whose
/// <see cref="ExpirationDate"/> is in the past is effectively expired.
/// Code that needs a single boolean answer should call
/// <see cref="IsCurrentlyValid"/>.
/// </para>
/// <para>
/// Soft-delete behaviour:
/// <list type="bullet">
///   <item>Removing a License goes through <c>SoftDeleteInterceptor</c> and only flips <c>IsDeleted</c>.</item>
///   <item>Removing the parent Provider cascades to all of its non-deleted Licenses.</item>
///   <item>The partial unique index <c>UX_Licenses_ProviderId_LicenseNumber_Active</c> means a license number can be reused after a soft-delete.</item>
/// </list>
/// </para>
/// </remarks>
public class License : BaseEntity
{
    /// <summary>Surrogate primary key.</summary>
    public int LicenseId { get; set; }

    /// <summary>Foreign key to the parent <see cref="Provider"/>. Required and immutable.</summary>
    public int ProviderId { get; set; }

    /// <summary>
    /// Business-recognisable license identifier. Unique per provider among
    /// non-deleted rows (enforced by a partial unique index).
    /// </summary>
    public string LicenseNumber { get; set; } = string.Empty;

    /// <summary>Recorded status — see <see cref="LicenseStatus"/>.</summary>
    public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.Active;

    /// <summary>Date past which the license is no longer valid (UTC date semantics).</summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>Reverse navigation to the parent <see cref="Provider"/>.</summary>
    public Provider Provider { get; set; } = null!;

    /// <summary>
    /// Computed validity flag — true only when status is
    /// <see cref="Domain.Enums.LicenseStatus.Active"/> AND the expiration
    /// date has not yet passed. This property is kept in domain rather
    /// than persisted so it can never go stale relative to the clock.
    /// EF is told to ignore it (see <c>LicenseConfiguration</c>).
    /// </summary>
    public bool IsCurrentlyValid =>
        LicenseStatus == LicenseStatus.Active && ExpirationDate.Date >= DateTime.UtcNow.Date;
}
