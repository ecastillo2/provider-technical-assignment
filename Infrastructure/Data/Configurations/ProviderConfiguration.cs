using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent EF Core configuration for the <see cref="Provider"/> aggregate.
/// </summary>
/// <remarks>
/// <para>
/// This file is the single source of truth for the Provider table's schema:
/// column types, constraints, indexes, and relationships. Keeping it
/// separate from the entity itself preserves the Domain layer's purity
/// (no EF references in <c>Domain/</c>) and makes schema changes easy to
/// review in isolation.
/// </para>
/// <para>
/// Notable design choices:
/// <list type="bullet">
///   <item>Status is persisted as TEXT with a <c>CHECK</c> constraint so the database itself rejects unknown values.</item>
///   <item>Composite indexes lead with <c>IsDeleted</c> so the global query filter can use them.</item>
///   <item>The Provider→Licenses relationship uses <see cref="DeleteBehavior.ClientCascade"/> — see the long comment near that line for why.</item>
/// </list>
/// </para>
/// </remarks>
public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        // ---------- Table + DB-level constraints ----------
        builder.ToTable("Providers", t =>
        {
            // Defence-in-depth: even if a future developer queries by raw
            // SQL or reaches the database via a different ORM, the column
            // values themselves are constrained. The string literals here
            // must match what HasConversion<string>() will write below.
            t.HasCheckConstraint(
                "CK_Providers_Status",
                "Status IN ('Active','Inactive','Pending')");
        });

        // ---------- Primary key ----------
        builder.HasKey(p => p.ProviderId);
        builder.Property(p => p.ProviderId).ValueGeneratedOnAdd();

        // ---------- Plain columns ----------
        builder.Property(p => p.ProviderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.County)
            .IsRequired()
            .HasMaxLength(100);

        // Persist enum as text rather than int. The database stays
        // self-describing and the CHECK constraint above can read it
        // directly. See ProviderStatus.cs for the full reasoning.
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedDate)
            .IsRequired();

        builder.Property(p => p.DeletedBy)
            .HasMaxLength(100);

        // ---------- Indexes (hot paths) ----------
        // The listing page sorts by name; the status filter narrows by
        // Status. Both queries also walk through the global query filter
        // (IsDeleted = 0), so leading every index with IsDeleted lets the
        // optimiser use the index for both the filter AND the projection.
        builder.HasIndex(p => new { p.IsDeleted, p.ProviderName })
            .HasDatabaseName("IX_Providers_IsDeleted_ProviderName");

        builder.HasIndex(p => new { p.IsDeleted, p.Status })
            .HasDatabaseName("IX_Providers_IsDeleted_Status");

        // ---------- Relationship to Licenses ----------
        builder.HasMany(p => p.Licenses)
            .WithOne(l => l.Provider)
            .HasForeignKey(l => l.ProviderId)
            // ClientCascade is the right fit for our soft-delete model:
            //
            //   * Database-level: NO cascade. We never hard-delete a row.
            //     At the DB schema level this maps to NO ACTION
            //     (equivalent to RESTRICT).
            //
            //   * Client-level: when a Provider's tracked state moves to
            //     Deleted, EF marks its tracked Licenses as Deleted too.
            //     Our SoftDeleteInterceptor then flips ALL Deleted
            //     entities (parent + children) to Modified+IsDeleted=true
            //     before SaveChanges issues any SQL. The result: a single
            //     transaction soft-deletes the parent and all loaded
            //     children. Untracked children are caught by an
            //     ExecuteUpdate fallback inside the interceptor.
            //
            // Why NOT Restrict here? Because Restrict makes EF reject
            // Remove(parent) synchronously when there are tracked
            // children with non-nullable FKs — the interceptor never
            // gets a chance to run. We hit this exact runtime exception
            // during smoke testing and switched to ClientCascade as the
            // fix.
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
