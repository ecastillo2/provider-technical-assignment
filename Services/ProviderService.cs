using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Infrastructure.Repositories;
using ProviderAssignmentStarter.Services.Mapping;
using ProviderAssignmentStarter.ViewModels.Providers;

namespace ProviderAssignmentStarter.Services;

public class ProviderService : IProviderService
{
    private readonly IProviderRepository _repo;

    public ProviderService(IProviderRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ProviderListItemVm>> ListAsync(CancellationToken ct = default)
    {
        // Single round-trip including Licenses so the listing grid can show
        // counts without N+1 queries.
        var providers = await _repo.GetAllWithLicensesAsync(ct);
        return providers.Select(p => p.ToListItem()).ToList();
    }

    public async Task<IReadOnlyList<ProviderListItemVm>> ListDeletedAsync(CancellationToken ct = default)
    {
        var deleted = await _repo.GetDeletedAsync(ct);
        return deleted.Select(p => p.ToListItem()).ToList();
    }

    public async Task<ProviderDetailsVm?> GetDetailsAsync(int providerId, CancellationToken ct = default)
    {
        var provider = await _repo.GetByIdWithLicensesAsync(providerId, ct);
        return provider?.ToDetailsVm();
    }

    public async Task<ProviderDetailsVm?> GetDetailsIncludingDeletedAsync(int providerId, CancellationToken ct = default)
    {
        var provider = await _repo.GetByIdIncludingDeletedAsync(providerId, ct);
        return provider?.ToDetailsVm();
    }

    public async Task<ProviderEditVm?> GetForEditAsync(int providerId, CancellationToken ct = default)
    {
        var provider = await _repo.GetByIdAsync(providerId, ct);
        return provider?.ToEditVm();
    }

    public async Task<int> CreateAsync(ProviderEditVm vm, CancellationToken ct = default)
    {
        var entity = new Provider
        {
            ProviderName = vm.ProviderName.Trim(),
            County       = vm.County.Trim(),
            Status       = vm.Status
        };
        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);
        return entity.ProviderId;
    }

    public async Task<bool> UpdateAsync(ProviderEditVm vm, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(vm.ProviderId, ct);
        if (existing is null) return false;

        existing.ProviderName = vm.ProviderName.Trim();
        existing.County       = vm.County.Trim();
        existing.Status       = vm.Status;

        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(int providerId, CancellationToken ct = default)
    {
        // Eager-load Licenses so the SoftDeleteInterceptor's tracked-cascade
        // path applies (more transparent in the change tracker than the bulk
        // ExecuteUpdate fallback, and surfaces errors earlier).
        var existing = await _repo.GetByIdWithLicensesAsync(providerId, ct);
        if (existing is null) return false;

        _repo.Remove(existing); // -> interceptor flips IsDeleted (parent + children)
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RestoreAsync(int providerId, CancellationToken ct = default)
    {
        var ok = await _repo.RestoreAsync(providerId, ct);
        if (ok) await _repo.SaveChangesAsync(ct);
        return ok;
    }
}
