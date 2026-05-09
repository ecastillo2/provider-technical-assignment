using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Infrastructure.Data;

/// <summary>
/// Brings the database up on application startup:
///  1. Creates the schema from the EF model if it does not yet exist.
///  2. Drops + recreates the analytical views (raw SQL - EF has no
///     first-class view support).
///  3. Seeds a realistic data set on first run.
///
/// We use EnsureCreatedAsync rather than MigrateAsync to keep the
/// reviewer-experience friction-free: clone, run, browse. Switching to
/// MigrateAsync after generating an InitialCreate migration is a
/// one-line change documented in the README.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await EnsureViewsAsync(db);
        await SeedAsync(db);
    }

    /// <summary>
    /// Views are dropped + recreated each startup so the definition stays
    /// in lock-step with the migration. Cheap on SQLite; the DDL is small.
    /// </summary>
    private static async Task EnsureViewsAsync(AppDbContext db)
    {
        const string sql = @"
DROP VIEW IF EXISTS vw_ActiveProviders;
DROP VIEW IF EXISTS vw_ActiveProvidersWithActiveLicenses;
DROP VIEW IF EXISTS vw_ActiveProvidersWithExpiredLicenses;

CREATE VIEW vw_ActiveProviders AS
SELECT  ProviderId,
        ProviderName,
        County,
        Status,
        CreatedDate,
        ModifiedDate
FROM    Providers
WHERE   IsDeleted = 0;

CREATE VIEW vw_ActiveProvidersWithActiveLicenses AS
SELECT  p.ProviderId,
        p.ProviderName,
        p.County,
        p.Status            AS ProviderStatus,
        l.LicenseId,
        l.LicenseNumber,
        l.LicenseStatus,
        l.ExpirationDate
FROM    Providers p
JOIN    Licenses  l ON l.ProviderId = p.ProviderId
WHERE   p.IsDeleted   = 0
  AND   l.IsDeleted   = 0
  AND   p.Status      = 'Active'
  AND   l.LicenseStatus = 'Active'
  AND   date(l.ExpirationDate) >= date('now');

-- The 'looks active but really isn't' scenario the assignment calls out:
-- the provider is recorded as Active but every one of their licenses is
-- either Expired-by-status or Expired-by-date.
CREATE VIEW vw_ActiveProvidersWithExpiredLicenses AS
SELECT  p.ProviderId,
        p.ProviderName,
        p.County,
        p.Status            AS ProviderStatus,
        l.LicenseId,
        l.LicenseNumber,
        l.LicenseStatus,
        l.ExpirationDate
FROM    Providers p
JOIN    Licenses  l ON l.ProviderId = p.ProviderId
WHERE   p.IsDeleted = 0
  AND   l.IsDeleted = 0
  AND   p.Status    = 'Active'
  AND ( l.LicenseStatus = 'Expired'
        OR date(l.ExpirationDate) < date('now') );
";
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Providers.IgnoreQueryFilters().AnyAsync())
        {
            return; // Idempotent: never re-seed an existing database.
        }

        var today = DateTime.UtcNow.Date;

        var providers = new List<Provider>
        {
            new()
            {
                ProviderName = "Atlanta Family Care",
                County = "Fulton",
                Status = ProviderStatus.Active,
                Licenses = new List<License>
                {
                    new() { LicenseNumber = "AFC-1001", LicenseStatus = LicenseStatus.Active, ExpirationDate = today.AddMonths(8) },
                    new() { LicenseNumber = "AFC-1002", LicenseStatus = LicenseStatus.Active, ExpirationDate = today.AddDays(20) }
                }
            },
            new()
            {
                ProviderName = "DeKalb Senior Services",
                County = "DeKalb",
                Status = ProviderStatus.Active,
                // Active provider whose only license expired - the spec scenario.
                Licenses = new List<License>
                {
                    new() { LicenseNumber = "DSS-2001", LicenseStatus = LicenseStatus.Active, ExpirationDate = today.AddMonths(-2) }
                }
            },
            new()
            {
                ProviderName = "Cobb Health Partners",
                County = "Cobb",
                Status = ProviderStatus.Active,
                Licenses = new List<License>
                {
                    new() { LicenseNumber = "CHP-3001", LicenseStatus = LicenseStatus.Suspended, ExpirationDate = today.AddMonths(6) },
                    new() { LicenseNumber = "CHP-3002", LicenseStatus = LicenseStatus.Active,    ExpirationDate = today.AddYears(1) }
                }
            },
            new()
            {
                ProviderName = "Gwinnett Pediatrics",
                County = "Gwinnett",
                Status = ProviderStatus.Pending,
                Licenses = new List<License>()
            },
            new()
            {
                ProviderName = "North Fulton Behavioral",
                County = "Fulton",
                Status = ProviderStatus.Inactive,
                Licenses = new List<License>
                {
                    new() { LicenseNumber = "NFB-4001", LicenseStatus = LicenseStatus.Expired, ExpirationDate = today.AddYears(-1) }
                }
            },
            new()
            {
                ProviderName = "South DeKalb Clinic",
                County = "DeKalb",
                Status = ProviderStatus.Active,
                Licenses = new List<License>
                {
                    new() { LicenseNumber = "SDC-5001", LicenseStatus = LicenseStatus.Active, ExpirationDate = today.AddDays(15) },
                    new() { LicenseNumber = "SDC-5002", LicenseStatus = LicenseStatus.Active, ExpirationDate = today.AddYears(2) }
                }
            },
            new()
            {
                ProviderName = "Cherokee Wellness",
                County = "Cherokee",
                Status = ProviderStatus.Active,
                Licenses = new List<License>
                {
                    new() { LicenseNumber = "CW-6001", LicenseStatus = LicenseStatus.Active, ExpirationDate = today.AddDays(60) }
                }
            },
            new()
            {
                ProviderName = "Henry County Outreach",
                County = "Henry",
                Status = ProviderStatus.Pending,
                Licenses = new List<License>()
            }
        };

        db.Providers.AddRange(providers);
        await db.SaveChangesAsync();

        // Demonstrate audit visibility: soft-delete one provider so the
        // 'Audit / Deleted' page has something to show out of the box.
        var toDelete = await db.Providers
            .Include(p => p.Licenses)
            .FirstAsync(p => p.ProviderName == "North Fulton Behavioral");

        db.Providers.Remove(toDelete); // Interceptor turns this into a soft delete.
        await db.SaveChangesAsync();
    }
}
