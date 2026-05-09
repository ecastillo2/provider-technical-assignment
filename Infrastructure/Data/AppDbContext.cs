using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Entities;

namespace ProviderAssignmentStarter.Infrastructure.Data;

/// <summary>
/// Single EF Core DbContext for the application.
///
/// Soft-delete enforcement (layer 2 of 3): a global query filter on every
/// soft-deletable entity excludes IsDeleted = true rows from every LINQ
/// query. Callers that need to see deleted rows (audit / restore) must
/// opt in by calling .IgnoreQueryFilters().
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<License> Licenses => Set<License>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Pull in every IEntityTypeConfiguration in this assembly. New
        // entities only need a configuration class; no edits here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters - enforced for every read.
        modelBuilder.Entity<Provider>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<License>().HasQueryFilter(l => !l.IsDeleted);
    }
}
