using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.Services;

/// <summary>
/// Application service for the Provider aggregate. Controllers depend on
/// this; this depends on IProviderRepository. Returns VMs only - never
/// leaks Domain entities into the web layer.
/// </summary>
public interface IProviderService
{
    Task<IReadOnlyList<ProviderListItemVm>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProviderListItemVm>> ListDeletedAsync(CancellationToken ct = default);
    Task<ProviderDetailsVm?> GetDetailsAsync(int providerId, CancellationToken ct = default);
    Task<ProviderDetailsVm?> GetDetailsIncludingDeletedAsync(int providerId, CancellationToken ct = default);
    Task<ProviderEditVm?> GetForEditAsync(int providerId, CancellationToken ct = default);

    Task<int> CreateAsync(ProviderEditVm vm, CancellationToken ct = default);
    Task<bool> UpdateAsync(ProviderEditVm vm, CancellationToken ct = default);

    /// <summary>Soft-deletes the provider and cascades to its licenses.</summary>
    Task<bool> SoftDeleteAsync(int providerId, CancellationToken ct = default);

    /// <summary>Audit / admin: undoes a soft-delete and restores cascaded licenses.</summary>
    Task<bool> RestoreAsync(int providerId, CancellationToken ct = default);
}
