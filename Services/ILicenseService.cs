using ProviderAssignmentStarter.ViewModels.Licenses;

namespace ProviderAssignmentStarter.Services;

public interface ILicenseService
{
    Task<LicenseEditVm?> GetForEditAsync(int licenseId, CancellationToken ct = default);
    Task<LicenseEditVm?> NewForProviderAsync(int providerId, CancellationToken ct = default);
    Task<int?> CreateAsync(LicenseEditVm vm, CancellationToken ct = default);
    Task<bool> UpdateAsync(LicenseEditVm vm, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int licenseId, CancellationToken ct = default);
    Task<LicenseListItemVm?> GetListItemAsync(int licenseId, CancellationToken ct = default);
}
