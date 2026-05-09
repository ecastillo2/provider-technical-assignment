using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent EF configuration for the License entity.
/// </summary>
public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("Licenses", t =>
        {
            t.HasCheckConstraint(
                "CK_Licenses_LicenseStatus",
                "LicenseStatus IN ('Active','Expired','Suspended')");
        });

        builder.HasKey(l => l.LicenseId);

        builder.Property(l => l.LicenseId)
            .ValueGeneratedOnAdd();

        builder.Property(l => l.LicenseNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.LicenseStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.ExpirationDate)
            .IsRequired();

        builder.Property(l => l.CreatedDate)
            .IsRequired();

        builder.Property(l => l.DeletedBy)
            .HasMaxLength(100);

        // IsCurrentlyValid is computed in domain — never persist it.
        builder.Ignore(l => l.IsCurrentlyValid);

        // Hot-path indexes: list licenses for a provider, find soon-expiring.
        builder.HasIndex(l => new { l.ProviderId, l.IsDeleted })
            .HasDatabaseName("IX_Licenses_ProviderId_IsDeleted");

        builder.HasIndex(l => l.ExpirationDate)
            .HasDatabaseName("IX_Licenses_ExpirationDate");

        // Business rule: license number must be unique per provider, but
        // only among non-deleted rows. Soft-deleted siblings are ignored
        // so a number can be reused after deletion. This is a SQLite
        // partial index — applied via raw SQL in the migration.
        builder.HasIndex(l => new { l.ProviderId, l.LicenseNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Licenses_ProviderId_LicenseNumber_Active");
    }
}
