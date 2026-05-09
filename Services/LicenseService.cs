using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Infrastructure.Repositories;
using ProviderAssignmentStarter.Services.Mapping;
using ProviderAssignmentStarter.ViewModels.Licenses;

namespace ProviderAssignmentStarter.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository  _licenseRepo;
    private readonly IProviderRepository _providerRepo;

    public LicenseService(ILicenseRepository licenseRepo, IProviderRepository providerRepo)
    {
        _licenseRepo  = licenseRepo;
        _providerRepo = providerRepo;
    }

    public async Task<LicenseEditVm?> GetForEditAsync(int licenseId, CancellationToken ct = default)
    {
        var license = await _licenseRepo.GetByIdAsync(licenseId, ct);
        return license?.ToEditVm(license.Provider?.ProviderName ?? string.Empty);
    }

    public async Task<LicenseEditVm?> NewForProviderAsync(int providerId, CancellationToken ct = default)
    {
        var provider = await _providerRepo.GetByIdAsync(providerId, ct);
        if (provider is null) return null;
        return new LicenseEditVm
        {
            ProviderId   = provider.ProviderId,
            ProviderName = provider.ProviderName
        };
    }

    public async Task<int?> CreateAsync(LicenseEditVm vm, CancellationToken ct = default)
    {
        var provider = await _providerRepo.GetByIdAsync(vm.ProviderId, ct);
        if (provider is null) return null;

        var license = new License
        {
            ProviderId     = vm.ProviderId,
            LicenseNumber  = vm.LicenseNumber.Trim(),
            LicenseStatus  = vm.LicenseStatus,
            ExpirationDate = vm.ExpirationDate
        };
        await _licenseRepo.AddAsync(license, ct);
        await _licenseRepo.SaveChangesAsync(ct);
        return license.LicenseId;
    }

    public async Task<bool> UpdateAsync(LicenseEditVm vm, CancellationToken ct = default)
    {
        var existing = await _licenseRepo.GetByIdAsync(vm.LicenseId, ct);
        if (existing is null) return false;

        existing.LicenseNumber  = vm.LicenseNumber.Trim();
        existing.LicenseStatus  = vm.LicenseStatus;
        existing.ExpirationDate = vm.ExpirationDate;

        await _licenseRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(int licenseId, CancellationToken ct = default)
    {
        var existing = await _licenseRepo.GetByIdAsync(licenseId, ct);
        if (existing is null) return false;

        _licenseRepo.Remove(existing); // -> interceptor soft-deletes
        await _licenseRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<LicenseListItemVm?> GetListItemAsync(int licenseId, CancellationToken ct = default)
    {
        var license = await _licenseRepo.GetByIdAsync(licenseId, ct);
        return license?.ToListItem(license.Provider?.ProviderName ?? string.Empty);
    }
}
