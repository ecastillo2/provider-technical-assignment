using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Repositories;

public interface ILicenseRepository
{
    Task<License?> GetByIdAsync(int licenseId, CancellationToken ct = default);
    Task<IReadOnlyList<License>> GetByProviderAsync(int providerId, CancellationToken ct = default);
    Task<IReadOnlyList<License>> GetExpiringSoonAsync(int days, CancellationToken ct = default);

    Task AddAsync(License license, CancellationToken ct = default);
    void Update(License license);
    void Remove(License license);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
