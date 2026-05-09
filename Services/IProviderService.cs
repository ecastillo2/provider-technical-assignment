using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.Services;

/// <summary>
/// Application service for the Provider aggregate.
/// </summary>
/// <remarks>
/// <para>
/// Sits between controllers and repositories. Controllers depend on this
/// interface; this depends on <see cref="Infrastructure.Repositories.IProviderRepository"/>.
/// </para>
/// <para>
/// Methods accept and return <b>view models only</b> — Domain entities
/// never leak into the web layer. This keeps the controller free of EF
/// concerns and lets the persistence schema change without rippling
/// through the views.
/// </para>
/// <para>
/// Cancellation tokens flow from <c>HttpContext.RequestAborted</c> down to
/// EF, so a closed browser tab cancels the in-flight query.
/// </para>
/// </remarks>
public interface IProviderService
{
    /// <summary>Listing for the main grid: non-deleted providers with license counts.</summary>
    Task<IReadOnlyList<ProviderListItemVm>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Filtered listing — search by name/county and/or filter by status.
    /// Same shape as <see cref="ListAsync"/>; just narrowed.
    /// </summary>
    Task<IReadOnlyList<ProviderListItemVm>> SearchAsync(
        ProviderListFilter filter,
        CancellationToken ct = default);

    /// <summary>Audit listing: soft-deleted providers only.</summary>
    Task<IReadOnlyList<ProviderListItemVm>> ListDeletedAsync(CancellationToken ct = default);

    /// <summary>Details VM for a non-deleted provider, or null if missing/deleted.</summary>
    Task<ProviderDetailsVm?> GetDetailsAsync(int providerId, CancellationToken ct = default);

    /// <summary>
    /// Audit-mode details: returns even soft-deleted records. Powers the
    /// Audit Details / Restore page.
    /// </summary>
    Task<ProviderDetailsVm?> GetDetailsIncludingDeletedAsync(int providerId, CancellationToken ct = default);

    /// <summary>VM pre-populated for the Edit form.</summary>
    Task<ProviderEditVm?> GetForEditAsync(int providerId, CancellationToken ct = default);

    /// <summary>Inserts a new provider. Returns the assigned id.</summary>
    Task<int> CreateAsync(ProviderEditVm vm, CancellationToken ct = default);

    /// <summary>Updates an existing provider. Returns false if the id was not found.</summary>
    Task<bool> UpdateAsync(ProviderEditVm vm, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes the provider and cascades the soft-delete to its
    /// Licenses (handled by <c>SoftDeleteInterceptor</c>). Returns false
    /// if the id was not found.
    /// </summary>
    Task<bool> SoftDeleteAsync(int providerId, CancellationToken ct = default);

    /// <summary>
    /// Audit / admin: undoes a soft-delete and restores cascaded Licenses.
    /// Returns false if the id is not currently soft-deleted.
    /// </summary>
    Task<bool> RestoreAsync(int providerId, CancellationToken ct = default);
}
