using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Tests.TestHelpers;
using Xunit;

namespace ProviderAssignmentStarter.Tests.Infrastructure;

/// <summary>
/// Verifies that CreatedDate / ModifiedDate are stamped automatically
/// and that CreatedDate is never overwritten on update.
/// </summary>
public class AuditableInterceptorTests
{
    [Fact]
    public async Task CreatedDate_is_stamped_on_insert()
    {
        using var test = new TestDb();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var provider = await test.AddAsync(SeedBuilder.NewProvider());

        provider.CreatedDate.Should().BeOnOrAfter(before);
        provider.ModifiedDate.Should().BeNull("ModifiedDate is only set on update");
    }

    [Fact]
    public async Task ModifiedDate_is_stamped_on_update_but_CreatedDate_is_preserved()
    {
        using var test = new TestDb();
        var provider = await test.AddAsync(SeedBuilder.NewProvider());
        var originalCreated = provider.CreatedDate;

        // Force a small gap so any accidental overwrite shows up.
        await Task.Delay(20);

        provider.ProviderName = "Renamed";
        await test.Context.SaveChangesAsync();

        provider.CreatedDate.Should().Be(originalCreated, "CreatedDate must never change after insert");
        provider.ModifiedDate.Should().NotBeNull();
        provider.ModifiedDate.Should().BeAfter(originalCreated);
    }
}
