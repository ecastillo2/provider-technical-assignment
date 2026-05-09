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
            // ClientCascade is the right fit for soft-delete:
            //   - Database-level: NO cascade. We never hard-delete a row,
            //     and the FK should reject orphaning. (At DB level this
            //     translates to NO ACTION, equivalent to RESTRICT.)
            //   - Client-level: when a Provider's tracked state goes to
            //     Deleted, EF marks its tracked Licenses as Deleted too.
            //     Our SoftDeleteInterceptor then flips ALL Deleted
            //     entities (parent + children) to Modified+IsDeleted=true.
            //
            // Restrict here would be incorrect: EF rejects Remove(parent)
            // synchronously because the children's required FK would be
            // severed - the interceptor never gets a chance to run.
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
