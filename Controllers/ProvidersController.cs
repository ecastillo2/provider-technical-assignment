using Microsoft.AspNetCore.Mvc;
using ProviderAssignmentStarter.Services;
using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.Controllers;

/// <summary>
/// CRUD for Providers. Stays thin: model state validation, then delegate
/// to <see cref="IProviderService"/>. No EF, no business logic in here.
/// </summary>
public class ProvidersController : Controller
{
    private readonly IProviderService _providers;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(IProviderService providers, ILogger<ProvidersController> logger)
    {
        _providers = providers;
        _logger    = logger;
    }

    // GET /Providers?Search=...&Status=Active
    // Filter values are bound from querystring so any filtered URL is shareable.
    public async Task<IActionResult> Index(
        [FromQuery] ProviderListFilter? filter,
        CancellationToken ct = default)
    {
        filter ??= new ProviderListFilter();

        var rows = filter.HasFilter
            ? await _providers.SearchAsync(filter, ct)
            : await _providers.ListAsync(ct);

        ViewData["Filter"] = filter;
        return View(rows);
    }

    // GET /Providers/Details/5
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var details = await _providers.GetDetailsAsync(id, ct);
        if (details is null) return NotFound();
        return View(details);
    }

    // GET /Providers/Create
    public IActionResult Create() => View(new ProviderEditVm());

    // POST /Providers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var newId = await _providers.CreateAsync(vm, ct);
        TempData["FlashSuccess"] = $"Provider '{vm.ProviderName}' created.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }

    // GET /Providers/Edit/5
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await _providers.GetForEditAsync(id, ct);
        if (vm is null) return NotFound();
        return View(vm);
    }

    // POST /Providers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProviderEditVm vm, CancellationToken ct)
    {
        if (id != vm.ProviderId) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var ok = await _providers.UpdateAsync(vm, ct);
        if (!ok) return NotFound();

        TempData["FlashSuccess"] = "Provider updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET /Providers/Delete/5  (confirmation page - GET never mutates)
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var details = await _providers.GetDetailsAsync(id, ct);
        if (details is null) return NotFound();
        return View(details);
    }

    // POST /Providers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var ok = await _providers.SoftDeleteAsync(id, ct);
        if (!ok) return NotFound();

        _logger.LogInformation("Provider {ProviderId} soft-deleted", id);
        TempData["FlashWarning"] = "Provider soft-deleted. Restore from the Audit page if needed.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Providers/Deleted  (audit listing)
    public async Task<IActionResult> Deleted(CancellationToken ct)
    {
        var rows = await _providers.ListDeletedAsync(ct);
        return View(rows);
    }

    // GET /Providers/AuditDetails/5  (read-only view of a deleted provider)
    public async Task<IActionResult> AuditDetails(int id, CancellationToken ct)
    {
        var details = await _providers.GetDetailsIncludingDeletedAsync(id, ct);
        if (details is null) return NotFound();
        return View(details);
    }

    // POST /Providers/Restore/5
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
