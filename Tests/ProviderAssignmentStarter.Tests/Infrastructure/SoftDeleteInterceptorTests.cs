using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.Tests.TestHelpers;
using Xunit;

namespace ProviderAssignmentStarter.Tests.Infrastructure;

/// <summary>
/// Verifies the no-hard-delete contract enforced by SoftDeleteInterceptor.
/// These tests run against a real SQLite database (in-memory) so the save
/// pipeline being exercised is identical to what runs in production.
/// </summary>
public class SoftDeleteInterceptorTests
{
    [Fact]
    public async Task Removing_a_Provider_does_not_actually_delete_the_row()
    {
        using var test = new TestDb();
        var provider = await test.AddAsync(SeedBuilder.NewProvider());

        test.Context.Providers.Remove(provider);
        await test.Context.SaveChangesAsync();

        // The row remains - hard-delete is impossible.
        var raw = await test.Context.Providers
            .IgnoreQueryFilters()
            .SingleAsync(p => p.ProviderId == provider.ProviderId);

        raw.IsDeleted.Should().BeTrue("the interceptor should flip IsDeleted instead of deleting");
        raw.DeletedDate.Should().NotBeNull();
        raw.DeletedBy.Should().Be("system");
    }

    [Fact]
    public async Task Soft_deleted_Providers_are_excluded_from_standard_queries()
    {
        using var test = new TestDb();
        var keep   = await test.AddAsync(SeedBuilder.NewProvider(name: "Keep"));
        var trash  = await test.AddAsync(SeedBuilder.NewProvider(name: "Trash"));

        test.Context.Providers.Remove(trash);
        await test.Context.SaveChangesAsync();

        var visible = await test.Context.Providers.ToListAsync();

        visible.Should().ContainSingle()
            .Which.ProviderName.Should().Be("Keep");
    }

    [Fact]
    public async Task IgnoreQueryFilters_surfaces_deleted_rows_for_audit()
    {
        using var test = new TestDb();
        var provider = await test.AddAsync(SeedBuilder.NewProvider(name: "Trash"));

        test.Context.Providers.Remove(provider);
        await test.Context.SaveChangesAsync();

        var auditView = await test.Context.Providers
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted)
            .ToListAsync();

        auditView.Should().ContainSingle()
            .Which.ProviderName.Should().Be("Trash");
    }

    [Fact]
    public async Task Cascade_soft_deletes_loaded_Licenses_when_Provider_is_removed()
    {
        using var test = new TestDb();

        var provider = await test.AddAsync(SeedBuilder.NewProvider(
            licenses: new[]
            {
                SeedBuilder.NewLicense("L-1"),
                SeedBuilder.NewLicense("L-2", LicenseStatus.Suspended)
            }));

        // Re-load with Licenses included (eager) so the interceptor's
        // tracked-cascade path is exercised.
        var loaded = await test.Context.Providers
            .Include(p => p.Licenses)
            .SingleAsync(p => p.ProviderId == provider.ProviderId);

        test.Context.Providers.Remove(loaded);
        await test.Context.SaveChangesAsync();

        var licenses = await test.Context.Licenses
            .IgnoreQueryFilters()
            .Where(l => l.ProviderId == provider.ProviderId)
            .ToListAsync();

        licenses.Should().HaveCount(2);
        licenses.Should().OnlyContain(l => l.IsDeleted,
            "cascade soft-delete should flip every child License");
        licenses.Should().OnlyContain(l => l.DeletedDate != null);
    }

    [Fact]
    public async Task Cascade_reaches_Licenses_that_were_not_eager_loaded()
    {
        using var test = new TestDb();

        var provider = await test.AddAsync(SeedBuilder.NewProvider(
            licenses: new[] { SeedBuilder.NewLicense("L-1"), SeedBuilder.NewLicense("L-2") }));

        // Detach everything so nothing is tracked, then re-fetch the
        // Provider WITHOUT Including Licenses. This is the realistic
        // case: a developer deletes a Provider without remembering to
        // load its children.
        test.Context.ChangeTracker.Clear();

        var bare = await test.Context.Providers.SingleAsync(p => p.ProviderId == provider.ProviderId);
        test.Context.Providers.Remove(bare);
        await test.Context.SaveChangesAsync();

        var licenses = await test.Context.Licenses
            .IgnoreQueryFilters()
            .Where(l => l.ProviderId == provider.ProviderId)
            .ToListAsync();

        licenses.Should().OnlyContain(l => l.IsDeleted,
            "the interceptor must also flip Licenses that were never loaded");
    }

    [Fact]
    public async Task Soft_deleted_Licenses_are_excluded_from_their_Provider_navigation()
    {
        using var test = new TestDb();
        var provider = await test.AddAsync(SeedBuilder.NewProvider(
            licenses: new[]
            {
                SeedBuilder.NewLicense("KEEP"),
                SeedBuilder.NewLicense("TRASH")
            }));

        var trashLicense = await test.Context.Licenses.SingleAsync(l => l.LicenseNumber == "TRASH");
        test.Context.Licenses.Remove(trashLicense);
        await test.Context.SaveChangesAsync();

        // Re-fetch with Include and verify the global query filter on
        // License hides the soft-deleted child.
        test.Context.ChangeTracker.Clear();
        var refreshed = await test.Context.Providers
            .Include(p => p.Licenses)
            .SingleAsync(p => p.ProviderId == provider.ProviderId);

        refreshed.Licenses.Should().ContainSingle()
            .Which.LicenseNumber.Should().Be("KEEP");
    }
}
