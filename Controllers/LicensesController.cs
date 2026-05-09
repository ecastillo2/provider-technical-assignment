using Microsoft.AspNetCore.Mvc;
using ProviderAssignmentStarter.Services;
using ProviderAssignmentStarter.ViewModels.Licenses;

namespace ProviderAssignmentStarter.Controllers;

/// <summary>
/// License CRUD. Routes are conceptually nested under a parent
/// <see cref="ProvidersController"/> via the <c>providerId</c> query/route
/// parameter; we redirect back to <c>Providers/Details/{id}</c> after
/// every successful mutation.
/// </summary>
/// <remarks>
/// <para>
/// Same thin-controller pattern as <see cref="ProvidersController"/>:
/// validate model, delegate to <see cref="ILicenseService"/>, redirect.
/// </para>
/// <para>
/// State-changing actions are POST-only with anti-forgery validation,
/// per ASP.NET Core best practice.
/// </para>
/// </remarks>
public class LicensesController : Controller
{
    private readonly ILicenseService _licenses;
    private readonly ILogger<LicensesController> _logger;

    public LicensesController(ILicenseService licenses, ILogger<LicensesController> logger)
    {
        _licenses = licenses;
        _logger   = logger;
    }

    // ============================================================
    //  Create (under a specific provider)
    // ============================================================

    /// <summary>
    /// GET /Licenses/Create?providerId=5 — empty form bound to the
    /// supplied provider.
    /// </summary>
    public async Task<IActionResult> Create(int providerId, CancellationToken ct)
    {
        var vm = await _licenses.NewForProviderAsync(providerId, ct);
        if (vm is null) return NotFound();
        return View(vm);
    }

    /// <summary>POST /Licenses/Create — insert and return to Provider Details.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LicenseEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var newId = await _licenses.CreateAsync(vm, ct);
        if (newId is null)
        {
            // Race condition: the parent provider was deleted between
            // the user opening the form and submitting it. Surface as
            // a model-level error so the user sees feedback rather than
            // a blank page.
            ModelState.AddModelError(string.Empty, "The associated provider could not be found.");
            return View(vm);
        }

        TempData["FlashSuccess"] = $"License '{vm.LicenseNumber}' added.";
        return RedirectToAction("Details", "Providers", new { id = vm.ProviderId });
    }

    // ============================================================
    //  Edit
    // ============================================================

    /// <summary>GET /Licenses/Edit/5 — pre-populated form.</summary>
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await _licenses.GetForEditAsync(id, ct);
        if (vm is null) return NotFound();
        return View(vm);
    }

    /// <summary>POST /Licenses/Edit/5</summary>
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

    // ============================================================
    //  Soft-delete (with confirmation)
    // ============================================================

    /// <summary>GET /Licenses/Delete/5 — confirmation page only; never mutates.</summary>
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await _licenses.GetListItemAsync(id, ct);
        if (item is null) return NotFound();
        return View(item);
    }

    /// <summary>POST /Licenses/Delete/5 — performs the soft-delete.</summary>
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
