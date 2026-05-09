using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Licenses;
using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.Services.Mapping;

/// <summary>
/// Hand-rolled extension methods that map Domain entities to ViewModels
/// (and back, where needed).
/// </summary>
/// <remarks>
/// <para>
/// Why not AutoMapper? With two aggregates and a half-dozen VMs, hand-
/// rolled mapping is a few lines of obvious code. AutoMapper would add
/// a configuration step, hide the projection from the reader, and
/// produce sub-optimal SQL when used inside <c>IQueryable.ProjectTo</c>.
/// Keeping the mappings here makes the conversion explicit and easy to
/// step through in the debugger.
/// </para>
/// <para>
/// Centralising the mappings here also means: when the schema or the VM
/// shape changes, there is exactly one file to update.
/// </para>
/// <para>
/// <b>Internal</b> visibility — these helpers are an implementation detail
/// of the Services layer; controllers should never call them directly.
/// </para>
/// </remarks>
internal static class EntityMappings
{
    /// <summary>
    /// Provider → list-item VM. Pre-computes license counts so the
    /// listing view stays dumb (no LINQ in Razor) and so the listing
    /// page never ships N+1 queries to production.
    /// </summary>
    public static ProviderListItemVm ToListItem(this Provider p) => new()
    {
        ProviderId          = p.ProviderId,
        ProviderName        = p.ProviderName,
        County              = p.County,
        Status              = p.Status,
        TotalLicenseCount   = p.Licenses.Count,
        ActiveLicenseCount  = p.Licenses.Count(l => l.LicenseStatus == LicenseStatus.Active
                                                    && l.ExpirationDate.Date >= DateTime.UtcNow.Date),
        ExpiredLicenseCount = p.Licenses.Count(l => l.LicenseStatus == LicenseStatus.Expired
                                                    || l.ExpirationDate.Date < DateTime.UtcNow.Date),
        CreatedDate         = p.CreatedDate,
        ModifiedDate        = p.ModifiedDate,
        IsDeleted           = p.IsDeleted,
        DeletedDate         = p.DeletedDate,
        DeletedBy           = p.DeletedBy
    };

    /// <summary>
    /// Provider → details VM, including a sorted list of license rows.
    /// Licenses are ordered by ExpirationDate ascending so the most
    /// imminent expirations appear first on the Details page.
    /// </summary>
    public static ProviderDetailsVm ToDetailsVm(this Provider p) => new()
    {
        ProviderId   = p.ProviderId,
        ProviderName = p.ProviderName,
        County       = p.County,
        Status       = p.Status,
        CreatedDate  = p.CreatedDate,
        ModifiedDate = p.ModifiedDate,
        IsDeleted    = p.IsDeleted,
        DeletedDate  = p.DeletedDate,
        DeletedBy    = p.DeletedBy,
        Licenses     = p.Licenses
            .OrderBy(l => l.ExpirationDate)
            .Select(l => l.ToListItem(p.ProviderName))
            .ToList()
    };

    /// <summary>Provider → form-bound Edit VM (no licenses, no audit columns).</summary>
    public static ProviderEditVm ToEditVm(this Provider p) => new()
    {
        ProviderId   = p.ProviderId,
        ProviderName = p.ProviderName,
        County       = p.County,
        Status       = p.Status
    };

    /// <summary>
    /// License → list-item VM. The provider name is passed in rather than
    /// pulled off <c>l.Provider</c> because callers may not have eager-
    /// loaded that navigation.
    /// </summary>
    public static LicenseListItemVm ToListItem(this License l, string providerName) => new()
    {
        LicenseId      = l.LicenseId,
        ProviderId     = l.ProviderId,
        ProviderName   = providerName,
        LicenseNumber  = l.LicenseNumber,
        LicenseStatus  = l.LicenseStatus,
        ExpirationDate = l.ExpirationDate
    };

    /// <summary>License → form-bound Edit VM.</summary>
    public static LicenseEditVm ToEditVm(this License l, string providerName) => new()
    {
        LicenseId      = l.LicenseId,
        ProviderId     = l.ProviderId,
        ProviderName   = providerName,
        LicenseNumber  = l.LicenseNumber,
        LicenseStatus  = l.LicenseStatus,
        ExpirationDate = l.ExpirationDate
    };
}
