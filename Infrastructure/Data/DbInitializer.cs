using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Infrastructure.Data;

/// <summary>
/// Brings the database up on application startup.
/// </summary>
/// <remarks>
/// <para>
/// Three responsibilities, executed in order via <see cref="InitializeAsync"/>:
/// </para>
/// <list type="number">
///   <item><b>Schema</b> — <c>EnsureCreatedAsync</c> creates the database from the EF model if no <c>.db</c> file exists.</item>
///   <item><b>Views</b> — drop + recreate the three analytical SQL views (EF has no first-class view support, so we use raw SQL).</item>
///   <item><b>Seed</b> — insert a realistic data set (idempotent: skips if any rows already exist).</item>
/// </list>
/// <para>
/// Why <c>EnsureCreatedAsync</c> and not <c>MigrateAsync</c>? It is a
/// deliberate trade-off in favour of friction-free reviewer experience.
/// A real project would use migrations; switching is a one-line change
/// (documented in the README §5). The hand-written SQL in
/// <c>Data/Sql/</c> mirrors what the EF model produces, so reviewers can
/// inspect schema without diving into EF tooling.
/// </para>
/// </remarks>
public static class DbInitializer
{
    /// <summary>
    /// Single entry point called from <c>Program.cs</c> at application
    /// startup. Idempotent — safe to run on every boot.
    /// </summary>
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await EnsureViewsAsync(db);
        await SeedAsync(db);
    }

    /// <summary>
    /// Drops + recreates the analytical views every startup. The DDL is
    /// tiny on SQLite, and recreating from scratch keeps the view
    /// definition in lock-step with the C# code without requiring a
    /// migration step.
    /// </summary>
    /// <remarks>
    /// Three views encode the assignment's required scenarios. The first
    /// is the "what should the app show by default" projection. The other
    /// two power dashboard widgets and audit queries.
    /// </remarks>
    private static async Task EnsureViewsAsync(AppDbContext db)
    {
        const string sql = @"
DROP VIEW IF EXISTS vw_ActiveProviders;
DROP VIEW IF EXISTS vw_ActiveProvidersWithActiveLicenses;
DROP VIEW IF EXISTS vw_ActiveProvidersWithExpiredLicenses;

-- vw_ActiveProviders --------------------------------------------------
-- The canonical 'what should the app show' projection. Mirrors the
-- DbContext global query filter; useful for raw-SQL reviewers.
CREATE VIEW vw_ActiveProviders AS
SELECT  ProviderId,
        ProviderName,
        County,
        Status,
        CreatedDate,
        ModifiedDate
FROM    Providers
WHERE   IsDeleted = 0;

-- vw_ActiveProvidersWithActiveLicenses --------------------------------
-- An Active provider AND a license that is currently valid (status
-- Active AND expiration in the future).
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

-- vw_ActiveProvidersWithExpiredLicenses -------------------------------
-- The 'looks active but really isn't' scenario the assignment calls
-- out: provider is recorded as Active, but EVERY one of their licenses
-- is either expired-by-status OR past its expiration date.
--
-- One row per qualifying provider. We deliberately do NOT join Licenses
-- in the SELECT — a join would multiply rows per provider AND would
-- silently change the semantics from 'all expired' to 'any expired'
-- (which is the bug a previous revision of this view shipped with).
-- The NOT EXISTS clause is what enforces the 'all expired' semantic:
-- a provider qualifies only when no currently-valid license exists.
CREATE VIEW vw_ActiveProvidersWithExpiredLicenses AS
SELECT  p.ProviderId,
        p.ProviderName,
        p.County,
        p.Status AS ProviderStatus
FROM    Providers p
WHERE   p.IsDeleted = 0
  AND   p.Status    = 'Active'
  AND   EXISTS (
            SELECT 1 FROM Licenses l
            WHERE l.ProviderId = p.ProviderId
              AND l.IsDeleted  = 0
        )
  AND NOT EXISTS (
            SELECT 1 FROM Licenses l
            WHERE l.ProviderId   = p.ProviderId
              AND l.IsDeleted    = 0
              AND l.LicenseStatus <> 'Expired'
              AND date(l.ExpirationDate) >= date('now')
        );
";
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    /// <summary>
    /// Seeds a realistic data set so reviewers see meaningful UI on first
    /// load. Idempotent: returns immediately if the database already has
    /// any provider rows (deleted or not).
    /// </summary>
    /// <remarks>
    /// The seed deliberately covers every assignment scenario:
    /// <list type="bullet">
    ///   <item>Multiple counties (Fulton, DeKalb, Cobb, Gwinnett, Cherokee, Henry).</item>
    ///   <item>All three provider statuses (Active, Inactive, Pending).</item>
    ///   <item>An Active provider whose only license is expired-by-date — the assignment's flagship scenario.</item>
    ///   <item>A Suspended license alongside Active and Expired ones.</item>
    ///   <item>A pre-soft-deleted provider so the Audit page has something to show on first run.</item>
    /// </list>
    /// </remarks>
    private static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Providers.IgnoreQueryFilters().AnyAsync())
        {
            // Idempotency guard: never re-seed an existing database.
            // Counts include soft-deleted rows so a half-seeded DB
            // (where everything was soft-deleted) still short-circuits.
            return;
        }

        var today = DateTime.UtcNow.Date;

        // Eight providers covering the full matrix of statuses and
        // counties. License mix is deliberately uneven.
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
                // Active provider whose only license has expired by date.
                // This is the 'looks active but really isn't' scenario
                // the assignment specifically calls out.
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
                Licenses = new List<License>() // Pending providers can have zero licenses.
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

        // Demonstrate the audit pathway: soft-delete one provider so
        // the Audit (Deleted) page has a record to show out of the box.
        // The interceptor handles cascading to the License.
        var toDelete = await db.Providers
            .Include(p => p.Licenses)
            .FirstAsync(p => p.ProviderName == "North Fulton Behavioral");

        db.Providers.Remove(toDelete);
        await db.SaveChangesAsync();
    }
}
