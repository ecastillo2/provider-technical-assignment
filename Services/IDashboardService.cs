using ProviderAssignmentStarter.ViewModels.Dashboard;

namespace ProviderAssignmentStarter.Services;

public interface IDashboardService
{
    Task<DashboardVm> BuildAsync(CancellationToken ct = default);
}
