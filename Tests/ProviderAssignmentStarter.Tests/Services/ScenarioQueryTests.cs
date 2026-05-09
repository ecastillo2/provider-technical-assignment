using FluentAssertions;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Infrastructure.Repositories;
using ProviderAssignmentStarter.Tests.TestHelpers;
using Xunit;

namespace ProviderAssignmentStarter.Tests.Services;

/// <summary>
/// Verifies the assignment's required data scenarios:
///   - Active providers + their currently-valid licenses
///   - Active providers whose licenses are all expired ("looks active but isn't")
/// </summary>
public class ScenarioQueryTests
{
    [Fact]
    public async Task GetActiveWithExpiredLicensesAsync_finds_active_providers_with_only_expired_licenses()
    {
        using var test = new TestDb();
        var today = DateTime.UtcNow.Date;

        // (a) Active provider whose only license expired by date.
        await test.AddAsync(SeedBuilder.NewProvider(
            name: "Looks Active But Expired",
            status: ProviderStatus.Active,
            licenses: new[] { SeedBuilder.NewLicense("X-1", LicenseStatus.Active, today.AddMonths(-1)) }));

        // (b) Active provider with a valid license - should NOT match.
        await test.AddAsync(SeedBuilder.NewProvider(
            name: "Genuinely Active",
            status: ProviderStatus.Active,
            licenses: new[] { SeedBuilder.NewLicense("Y-1", LicenseStatus.Active, today.AddMonths(6)) }));

        // (c) Inactive provider with an expired license - should NOT match
        // (we filter on Provider.Status = Active).
        await test.AddAsync(SeedBuilder.NewProvider(
            name: "Inactive Anyway",
            status: ProviderStatus.Inactive,
            licenses: new[] { SeedBuilder.NewLicense("Z-1", LicenseStatus.Expired, today.AddMonths(-3)) }));

        var repo = new ProviderRepository(test.Context);
        var results = await repo.GetActiveWithExpiredLicensesAsync();

        results.Should().ContainSingle()
            .Which.ProviderName.Should().Be("Looks Active But Expired");
    }

    [Fact]
    public async Task GetActiveWithActiveLicensesAsync_returns_active_providers_with_currently_valid_licenses()
    {
        using var test = new TestDb();
        var today = DateTime.UtcNow.Date;

        // Active provider with one valid license and one expired license.
        // Expected: provider returned with only the valid license attached.
        await test.AddAsync(SeedBuilder.NewProvider(
            name: "Mixed",
            status: ProviderStatus.Active,
            licenses: new[]
            {
                SeedBuilder.NewLicense("VALID",   LicenseStatus.Active, today.AddYears(1)),
                SeedBuilder.NewLicense("EXPIRED", LicenseStatus.Active, today.AddDays(-10))
            }));

        // Pending provider should never appear.
        await test.AddAsync(SeedBuilder.NewProvider(
            name: "Pending",
            status: ProviderStatus.Pending,
            licenses: new[] { SeedBuilder.NewLicense("P-1", LicenseStatus.Active, today.AddYears(1)) }));

        var repo = new ProviderRepository(test.Context);
        var results = await repo.GetActiveWithActiveLicensesAsync();

        results.Should().ContainSingle()
            .Which.ProviderName.Should().Be("Mixed");
        results[0].Licenses.Should().ContainSingle()
            .Which.LicenseNumber.Should().Be("VALID");
    }
}
