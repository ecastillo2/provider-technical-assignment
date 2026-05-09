using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Data;

/// <summary>
/// Single Entity Framework Core context for the application.
/// </summary>
/// <remarks>
/// <para>
/// <b>Soft-delete enforcement (layer 2 of 3)</b> happens here, via the
/// global query filters configured in <see cref="OnModelCreating"/>:
/// </para>
/// <code>
/// modelBuilder.Entity&lt;Provider&gt;().HasQueryFilter(p =&gt; !p.IsDeleted);
/// modelBuilder.Entity&lt;License&gt;().HasQueryFilter(l =&gt; !l.IsDeleted);
/// </code>
/// <para>
/// Every LINQ query against <see cref="Providers"/> or <see cref="Licenses"/>
/// silently appends <c>WHERE IsDeleted = 0</c>. This means the application
/// CANNOT accidentally surface a deleted record from a normal read; only
/// callers that explicitly opt out via <c>.IgnoreQueryFilters()</c> will
/// see deleted rows. Those call sites are confined to repository methods
/// that begin with <c>GetDeleted...</c> or <c>...IncludingDeleted...</c>
/// so the audit pathway is grep-able.
/// </para>
/// <para>
/// Layer 1 of soft-delete enforcement is the <c>ISoftDeletable</c>
/// contract on <c>BaseEntity</c>. Layer 3 is <c>SoftDeleteInterceptor</c>,
/// registered in <c>Program.cs</c> via <c>options.AddInterceptors(...)</c>.
/// </para>
/// <para>
/// Per-entity schema concerns (column types, indexes, FK behaviour, CHECK
/// constraints, partial unique indexes) live in
/// <c>Configurations/&lt;Entity&gt;Configuration.cs</c>. They are picked up
/// automatically by <c>ApplyConfigurationsFromAssembly(...)</c> below, so
/// adding a new entity is a matter of writing one new configuration class
/// — no edits to this file required.
/// </para>
/// </remarks>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Provider aggregate root. Filtered to exclude soft-deleted rows.</summary>
    public DbSet<Provider> Providers => Set<Provider>();

    /// <summary>License aggregate (child of Provider). Filtered to exclude soft-deleted rows.</summary>
    public DbSet<License> Licenses => Set<License>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Pull in every IEntityTypeConfiguration<T> defined in this
        // assembly. New entities only need a configuration class; no
        // edits here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ---------- Layer 2 of soft-delete enforcement ----------
        // Every standard read silently excludes soft-deleted rows.
        // Callers that need the full set must call .IgnoreQueryFilters().
        modelBuilder.Entity<Provider>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<License>().HasQueryFilter(l => !l.IsDeleted);
    }
}
