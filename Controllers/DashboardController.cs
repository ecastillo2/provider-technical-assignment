using Microsoft.AspNetCore.Mvc;
using ProviderAssignmentStarter.Services;

namespace ProviderAssignmentStarter.Controllers;

/// <summary>
/// Renders the read-only dashboard at <c>/Dashboard</c>.
/// </summary>
/// <remarks>
/// <para>
/// All metrics are computed by <see cref="IDashboardService"/> in C#;
/// this controller just builds the VM and hands it to the Razor view.
/// The view contains no business rules — only Bootstrap layout and
/// Chart.js configuration.
/// </para>
/// <para>
/// The dashboard never mutates state, so there is no POST action and
/// no anti-forgery token needed.
/// </para>
/// </remarks>
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
