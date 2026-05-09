using Microsoft.AspNetCore.Mvc;
using ProviderAssignmentStarter.Services;
using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.Controllers;

/// <summary>
/// Routes Provider CRUD requests to <see cref="IProviderService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Stays deliberately thin. Each action does at most three things:
/// </para>
/// <list type="number">
///   <item>Validate <c>ModelState</c> and bind the VM.</item>
///   <item>Delegate to the service.</item>
///   <item>Return the right view / redirect.</item>
/// </list>
/// <para>
/// No EF, no LINQ, no business rules. The controller does not reference
/// any class from the Infrastructure layer — only services and VMs.
/// All POST actions carry <c>[ValidateAntiForgeryToken]</c> and follow
/// the POST-Redirect-GET pattern so a refresh never re-submits.
/// </para>
/// <para>
/// Cancellation tokens come from <c>HttpContext.RequestAborted</c> and
/// flow all the way down to EF.
/// </para>
/// </remarks>
public class ProvidersController : Controller
{
    private readonly IProviderService _providers;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(IProviderService providers, ILogger<ProvidersController> logger)
    {
        _providers = providers;
        _logger    = logger;
    }

    // ============================================================
    //  Standard listing + filter
    // ============================================================

    /// <summary>
    /// GET /Providers?Search=...&amp;Status=Active
    /// Filter values are bound from the querystring so any filtered URL
    /// is shareable / bookmarkable. An absent filter falls through to
    /// the unfiltered listing.
    /// </summary>
    public async Task<IActionResult> Index(
        [FromQuery] ProviderListFilter? filter,
        CancellationToken ct = default)
    {
        filter ??= new ProviderListFilter();

        var rows = filter.HasFilter
            ? await _providers.SearchAsync(filter, ct)
            : await _providers.ListAsync(ct);

        // Stash the filter in ViewData so the form on the page can
        // re-render its inputs with the current values.
        ViewData["Filter"] = filter;
        return View(rows);
    }

    /// <summary>GET /Providers/Details/5</summary>
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var details = await _providers.GetDetailsAsync(id, ct);
        if (details is null) return NotFound();
        return View(details);
    }

    // ============================================================
    //  Create
    // ============================================================

    /// <summary>GET /Providers/Create — empty form.</summary>
    public IActionResult Create() => View(new ProviderEditVm());

    /// <summary>POST /Providers/Create — insert and redirect to Details.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var newId = await _providers.CreateAsync(vm, ct);
        // TempData survives a single redirect; the layout renders it as
        // a flash banner on the destination page.
        TempData["FlashSuccess"] = $"Provider '{vm.ProviderName}' created.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }

    // ============================================================
    //  Edit
    // ============================================================

    /// <summary>GET /Providers/Edit/5 — pre-populated form.</summary>
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await _providers.GetForEditAsync(id, ct);
        if (vm is null) return NotFound();
        return View(vm);
    }

    /// <summary>POST /Providers/Edit/5</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProviderEditVm vm, CancellationToken ct)
    {
        // Defence-in-depth against tampered route values.
        if (id != vm.ProviderId) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var ok = await _providers.UpdateAsync(vm, ct);
        if (!ok) return NotFound();

        TempData["FlashSuccess"] = "Provider updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    //  Soft-delete (with confirmation)
    // ============================================================

    /// <summary>
    /// GET /Providers/Delete/5 — confirmation page. GET never mutates;
    /// the actual delete happens on POST below.
    /// </summary>
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var details = await _providers.GetDetailsAsync(id, ct);
        if (details is null) return NotFound();
        return View(details);
    }

    /// <summary>POST /Providers/Delete/5 — performs the soft-delete.</summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var ok = await _providers.SoftDeleteAsync(id, ct);
        if (!ok) return NotFound();

        // Audit-trail breadcrumb. In a real system this would also write
        // to a dedicated AuditLog table.
        _logger.LogInformation("Provider {ProviderId} soft-deleted", id);
        TempData["FlashWarning"] = "Provider soft-deleted. Restore from the Audit page if needed.";
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    //  Audit (Deleted) listing + restore
    // ============================================================

    /// <summary>GET /Providers/Deleted — audit listing of soft-deleted providers.</summary>
    public async Task<IActionResult> Deleted(CancellationToken ct)
    {
        var rows = await _providers.ListDeletedAsync(ct);
        return View(rows);
    }

    /// <summary>
    /// GET /Providers/AuditDetails/5 — read-only view of a soft-deleted
    /// provider, with its (also soft-deleted) Licenses included.
    /// </summary>
    public async Task<IActionResult> AuditDetails(int id, CancellationToken ct)
    {
        var details = await _providers.GetDetailsIncludingDeletedAsync(id, ct);
        if (details is null) return NotFound();
        return View(details);
    }

    /// <summary>
    /// POST /Providers/Restore/5 — reverses a soft-delete. The cascade
    /// is reversed too: cascaded-deleted Licenses are restored alongside
    /// the parent.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var ok = await _providers.RestoreAsync(id, ct);
        if (!ok) return NotFound();

        TempData["FlashSuccess"] = "Provider restored.";
        return RedirectToAction(nameof(Index));
    }
}
