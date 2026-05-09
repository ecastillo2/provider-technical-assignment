-- =====================================================================
-- 002_create_views.sql
-- Reusable views that encode the soft-delete rule and the analytical
-- queries the assignment calls out as required scenarios.
--
-- Note: SQLite parses date('now') in UTC and accepts ISO-8601 strings
-- in TEXT date columns - matching what EF Core writes for DateTime.
-- =====================================================================

DROP VIEW IF EXISTS vw_ActiveProviders;
DROP VIEW IF EXISTS vw_ActiveProvidersWithActiveLicenses;
DROP VIEW IF EXISTS vw_ActiveProvidersWithExpiredLicenses;

-- ---------------------------------------------------------------------
-- vw_ActiveProviders
-- The canonical "what should the app show" projection - soft-deleted
-- rows are excluded. Mirrors the DbContext global query filter.
-- ---------------------------------------------------------------------
CREATE VIEW vw_ActiveProviders AS
SELECT  ProviderId,
        ProviderName,
        County,
        Status,
        CreatedDate,
        ModifiedDate
FROM    Providers
WHERE   IsDeleted = 0;

-- ---------------------------------------------------------------------
-- vw_ActiveProvidersWithActiveLicenses
-- Active provider rows joined to currently-valid licenses. A license
-- is "currently valid" iff its status is Active AND its expiration
-- date is today or later.
-- ---------------------------------------------------------------------
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
WHERE   p.IsDeleted     = 0
  AND   l.IsDeleted     = 0
  AND   p.Status        = 'Active'
  AND   l.LicenseStatus = 'Active'
  AND   date(l.ExpirationDate) >= date('now');

-- ---------------------------------------------------------------------
-- vw_ActiveProvidersWithExpiredLicenses
-- The 'looks active but really isn't' scenario the spec calls out:
-- the provider is recorded as Active, but every one of their licenses
-- is either Expired by status OR past its expiration date.
-- ---------------------------------------------------------------------
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
