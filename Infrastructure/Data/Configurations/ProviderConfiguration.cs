using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent EF configuration for the Provider aggregate. Kept separate from
/// the entity so Domain stays pure (no EF references) and so all schema
/// concerns live in one auditable place.
/// </summary>
public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Providers", t =>
        {
            // Defence-in-depth: even if a future developer queries by raw
            // SQL, the database itself rejects bogus status values.
            t.HasCheckConstraint(
                "CK_Providers_Status",
                "Status IN ('Active','Inactive','Pending')");
        });

        builder.HasKey(p => p.ProviderId);

        builder.Property(p => p.ProviderId)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.ProviderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.County)
            .IsRequired()
            .HasMaxLength(100);

        // Persist enum as text — the database stays self-describing and
        // the CHECK constraint above can read it directly.
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedDate)
            .IsRequired();

        builder.Property(p => p.DeletedBy)
            .HasMaxLength(100);

        // Indexes targeted at the hot path: list of non-deleted providers
        // filtered/sorted by name and status.
        builder.HasIndex(p => new { p.IsDeleted, p.ProviderName })
            .HasDatabaseName("IX_Providers_IsDeleted_ProviderName");

        builder.HasIndex(p => new { p.IsDeleted, p.Status })
            .HasDatabaseName("IX_Providers_IsDeleted_Status");

        builder.HasMany(p => p.Licenses)
            .WithOne(l => l.Provider)
            .HasForeignKey(l => l.ProviderId)
            // We never hard-delete; cascade-delete would conflict with
            // soft-delete semantics. The interceptor handles cascading.
            .OnDelete(DeleteBehavior.Restrict);
    }
}
