using Microsoft.AspNetCore.Mvc;
using ProviderAssignmentStarter.Services;
using ProviderAssignmentStarter.ViewModels.Licenses;

namespace ProviderAssignmentStarter.Controllers;

/// <summary>
/// License CRUD. Routes are nested conceptually under a Provider via the
/// providerId query/route parameter. We POST-only for any state change.
/// </summary>
public class LicensesController : Controller
{
    private readonly ILicenseService _licenses;
    private readonly ILogger<LicensesController> _logger;

    public LicensesController(ILicenseService licenses, ILogger<LicensesController> logger)
    {
        _licenses = licenses;
        _logger   = logger;
    }

    // GET /Licenses/Create?providerId=5
    public async Task<IActionResult> Create(int providerId, CancellationToken ct)
    {
        var vm = await _licenses.NewForProviderAsync(providerId, ct);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LicenseEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var newId = await _licenses.CreateAsync(vm, ct);
        if (newId is null)
        {
            ModelState.AddModelError(string.Empty, "The associated provider could not be found.");
            return View(vm);
        }

        TempData["FlashSuccess"] = $"License '{vm.LicenseNumber}' added.";
        return RedirectToAction("Details", "Providers", new { id = vm.ProviderId });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await _licenses.GetForEditAsync(id, ct);
        if (vm is null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LicenseEditVm vm, CancellationToken ct)
    {
        if (id != vm.LicenseId) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var ok = await _licenses.UpdateAsync(vm, ct);
        if (!ok) return NotFound();

        TempData["FlashSuccess"] = "License updated.";
        return RedirectToAction("Details", "Providers", new { id = vm.ProviderId });
    }

    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await _licenses.GetListItemAsync(id, ct);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, int providerId, CancellationToken ct)
    {
        var ok = await _licenses.SoftDeleteAsync(id, ct);
        if (!ok) return NotFound();

        _logger.LogInformation("License {LicenseId} soft-deleted", id);
        TempData["FlashWarning"] = "License soft-deleted.";
        return RedirectToAction("Details", "Providers", new { id = providerId });
    }
}
