using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent EF Core configuration for the <see cref="License"/> entity.
/// </summary>
/// <remarks>
/// Single source of truth for the Licenses table's schema. Same pattern
/// as <see cref="ProviderConfiguration"/>; see that class for the broader
/// rationale.
/// </remarks>
public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        // ---------- Table + DB-level constraints ----------
        builder.ToTable("Licenses", t =>
        {
            // CHECK constraint mirrors the LicenseStatus enum members.
            t.HasCheckConstraint(
                "CK_Licenses_LicenseStatus",
                "LicenseStatus IN ('Active','Expired','Suspended')");
        });

        // ---------- Primary key ----------
        builder.HasKey(l => l.LicenseId);
        builder.Property(l => l.LicenseId).ValueGeneratedOnAdd();

        // ---------- Plain columns ----------
        builder.Property(l => l.LicenseNumber)
            .IsRequired()
            .HasMaxLength(50);

        // Enum-as-text matches ProviderStatus's strategy.
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

        // The IsCurrentlyValid getter is computed in domain - never persist
        // it. Persisting would risk staleness as the wall clock advances.
        builder.Ignore(l => l.IsCurrentlyValid);

        // ---------- Indexes (hot paths) ----------

        // Per-provider license listing on the Provider Details page.
        builder.HasIndex(l => new { l.ProviderId, l.IsDeleted })
            .HasDatabaseName("IX_Licenses_ProviderId_IsDeleted");

        // Dashboard's "expiring in next 30 days" widget filters by
        // ExpirationDate; an index makes that range scan cheap.
        builder.HasIndex(l => l.ExpirationDate)
            .HasDatabaseName("IX_Licenses_ExpirationDate");

        // Business rule: a license number must be unique per provider,
        // BUT only among non-deleted rows. Soft-deleted siblings are
        // ignored so a number can be reused after a deletion. This is
        // expressed via a SQLite partial index, which EF Core emits via
        // HasFilter("[IsDeleted] = 0").
        builder.HasIndex(l => new { l.ProviderId, l.LicenseNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Licenses_ProviderId_LicenseNumber_Active");
    }
}
