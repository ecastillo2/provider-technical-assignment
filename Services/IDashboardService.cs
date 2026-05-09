using ProviderAssignmentStarter.ViewModels.Dashboard;

namespace ProviderAssignmentStarter.Services;

/// <summary>
/// Builds the <see cref="DashboardVm"/> for the dashboard page.
/// </summary>
/// <remarks>
/// Every metric on the dashboard is computed here, in C#, against the
/// repositories. Per the assignment: business logic must not exist only
/// in the UI. The Razor view at <c>Views/Dashboard/Index.cshtml</c>
/// receives a fully-populated VM and only renders.
/// </remarks>
public interface IDashboardService
{
    Task<DashboardVm> BuildAsync(CancellationToken ct = default);
}
