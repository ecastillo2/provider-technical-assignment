using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Licenses;
using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.Services.Mapping;

/// <summary>
/// Hand-rolled entity-to-VM mapping. Kept centralised so when the schema
/// or the VM shape changes, there is exactly one place to update. AutoMapper
/// would be overkill for two aggregates and would obscure the projection.
/// </summary>
internal static class EntityMappings
{
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

    public static ProviderEditVm ToEditVm(this Provider p) => new()
    {
        ProviderId   = p.ProviderId,
        ProviderName = p.ProviderName,
        County       = p.County,
        Status       = p.Status
    };

    public static LicenseListItemVm ToListItem(this License l, string providerName) => new()
    {
        LicenseId      = l.LicenseId,
        ProviderId     = l.ProviderId,
        ProviderName   = providerName,
        LicenseNumber  = l.LicenseNumber,
        LicenseStatus  = l.LicenseStatus,
        ExpirationDate = l.ExpirationDate
    };

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
