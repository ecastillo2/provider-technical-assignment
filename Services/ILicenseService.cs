using ProviderAssignmentStarter.ViewModels.Licenses;

namespace ProviderAssignmentStarter.Services;

/// <summary>
/// Application service for the <see cref="Domain.Entities.License"/> entity.
/// </summary>
/// <remarks>
/// Same shape and rationale as <see cref="IProviderService"/> — VMs in,
/// VMs out, no EF leakage. License is a child of Provider so most
/// operations also need a parent-provider lookup; the service handles
/// that internally.
/// </remarks>
public interface ILicenseService
{
    /// <summary>VM pre-populated for the License Edit form.</summary>
    Task<LicenseEditVm?> GetForEditAsync(int licenseId, CancellationToken ct = default);

    /// <summary>
    /// Fresh empty VM tied to a specific provider, used by the License
    /// Create form. Returns null if the provider does not exist.
    /// </summary>
    Task<LicenseEditVm?> NewForProviderAsync(int providerId, CancellationToken ct = default);

    /// <summary>
    /// Inserts a new License under the supplied provider. Returns the
    /// new license's id, or null if the provider id was not found
    /// (so the controller can ModelState-error rather than 404).
    /// </summary>
    Task<int?> CreateAsync(LicenseEditVm vm, CancellationToken ct = default);

    /// <summary>Updates an existing license. Returns false if the id was not found.</summary>
    Task<bool> UpdateAsync(LicenseEditVm vm, CancellationToken ct = default);

    /// <summary>Soft-deletes a single license. Does not touch the parent.</summary>
    Task<bool> SoftDeleteAsync(int licenseId, CancellationToken ct = default);

    /// <summary>Read-only list-item VM (used by the Delete confirmation page).</summary>
    Task<LicenseListItemVm?> GetListItemAsync(int licenseId, CancellationToken ct = default);
}
