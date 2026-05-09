using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Infrastructure.Repositories;
using ProviderAssignmentStarter.Services;
using ProviderAssignmentStarter.Tests.TestHelpers;
using ProviderAssignmentStarter.ViewModels.Providers;
using Xunit;

namespace ProviderAssignmentStarter.Tests.Services;

/// <summary>
/// Exercises ProviderService end-to-end against a real SQLite database.
/// These tests don't use mocks - they validate the entire pipeline that
/// runs in production: service → repository → DbContext → interceptors.
/// </summary>
public class ProviderServiceTests
{
    private static (IProviderService service, TestDb test) CreateSut()
    {
        var test = new TestDb();
        var repo = new ProviderRepository(test.Context);
        var service = new ProviderService(repo);
        return (service, test);
    }

    [Fact]
    public async Task CreateAsync_persists_a_new_provider_and_returns_its_id()
    {
        var (service, test) = CreateSut();
        using var _ = test;

        var newId = await service.CreateAsync(new ProviderEditVm
        {
            ProviderName = "Atlanta Family Care",
            County       = "Fulton",
            Status       = ProviderStatus.Active
        });

        newId.Should().BeGreaterThan(0);

        var saved = await test.Context.Providers.SingleAsync();
        saved.ProviderName.Should().Be("Atlanta Family Care");
        saved.County.Should().Be("Fulton");
        saved.Status.Should().Be(ProviderStatus.Active);
    }

    [Fact]
    public async Task UpdateAsync_persists_changes_and_bumps_ModifiedDate()
    {
        var (service, test) = CreateSut();
        using var _ = test;

        var id = await service.CreateAsync(new ProviderEditVm
        {
            ProviderName = "Original",
            County       = "Fulton",
            Status       = ProviderStatus.Pending
        });

        await Task.Delay(20);

        var ok = await service.UpdateAsync(new ProviderEditVm
        {
            ProviderId   = id,
            ProviderName = "Renamed",
            County       = "DeKalb",
            Status       = ProviderStatus.Active
        });

        ok.Should().BeTrue();

        var refreshed = await test.Context.Providers.SingleAsync();
        refreshed.ProviderName.Should().Be("Renamed");
        refreshed.County.Should().Be("DeKalb");
        refreshed.Status.Should().Be(ProviderStatus.Active);
        refreshed.ModifiedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_hides_provider_from_listing_and_cascades_to_licenses()
    {
        var (service, test) = CreateSut();
        using var _ = test;

        var seed = await test.AddAsync(SeedBuilder.NewProvider(
            name: "ToDelete",
            licenses: new[] { SeedBuilder.NewLicense("L-1"), SeedBuilder.NewLicense("L-2") }));

        var ok = await service.SoftDeleteAsync(seed.ProviderId);
        ok.Should().BeTrue();

        // Standard listing must hide it.
        var visible = await service.ListAsync();
        visible.Should().BeEmpty();

        // Audit listing must surface it.
        var deleted = await service.ListDeletedAsync();
        deleted.Should().ContainSingle()
            .Which.ProviderName.Should().Be("ToDelete");

        // Cascade must have flipped both Licenses.
        var licenses = await test.Context.Licenses
            .IgnoreQueryFilters()
            .Where(l => l.ProviderId == seed.ProviderId)
            .ToListAsync();
        licenses.Should().OnlyContain(l => l.IsDeleted);
    }

    [Fact]
    public async Task RestoreAsync_undeletes_provider_and_its_cascaded_licenses()
    {
        var (service, test) = CreateSut();
        using var _ = test;

        var seed = await test.AddAsync(SeedBuilder.NewProvider(
            name: "Restorable",
            licenses: new[] { SeedBuilder.NewLicense("L-1") }));

        await service.SoftDeleteAsync(seed.ProviderId);

        var ok = await service.RestoreAsync(seed.ProviderId);
        ok.Should().BeTrue();

        var visible = await service.ListAsync();
        visible.Should().ContainSingle()
            .Which.ProviderName.Should().Be("Restorable");

        var licenses = await test.Context.Licenses
            .Where(l => l.ProviderId == seed.ProviderId)
            .ToListAsync();
        licenses.Should().HaveCount(1);
        licenses.Should().OnlyContain(l => !l.IsDeleted, "Restore should reverse the cascade");
    }

    [Fact]
    public async Task GetDetailsAsync_returns_null_for_a_soft_deleted_provider()
    {
        var (service, test) = CreateSut();
        using var _ = test;

        var seed = await test.AddAsync(SeedBuilder.NewProvider());
        await service.SoftDeleteAsync(seed.ProviderId);

        var details = await service.GetDetailsAsync(seed.ProviderId);
        details.Should().BeNull("the global query filter must hide deleted rows from standard reads");
    }

    [Fact]
    public async Task GetDetailsIncludingDeletedAsync_returns_a_soft_deleted_provider()
    {
        var (service, test) = CreateSut();
        using var _ = test;

        var seed = await test.AddAsync(SeedBuilder.NewProvider(name: "Audited"));
        await service.SoftDeleteAsync(seed.ProviderId);

        var details = await service.GetDetailsIncludingDeletedAsync(seed.ProviderId);
        details.Should().NotBeNull();
        details!.ProviderName.Should().Be("Audited");
        details.IsDeleted.Should().BeTrue();
    }
}
