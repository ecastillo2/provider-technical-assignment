using Microsoft.AspNetCore.Mvc;
using ProviderAssignmentStarter.Services;

namespace ProviderAssignmentStarter.Controllers;

/// <summary>
/// Read-only dashboard. All metrics are computed by DashboardService;
/// the view only renders.
/// </summary>
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = await _dashboard.BuildAsync(ct);
        return View(vm);
    }
}
