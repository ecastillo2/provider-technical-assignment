-- =====================================================================
-- 001_create_tables.sql
-- Schema for the Provider Management application (SQLite).
--
-- This script is the hand-written equivalent of the EF Core migration
-- at /Migrations/*_InitialCreate.cs. Either path produces the same
-- schema. The application normally relies on EF migrations at startup;
-- this file exists to document the schema independently and to satisfy
-- the assignment's requirement to ship SQL scripts.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Providers
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Providers (
    ProviderId      INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderName    TEXT     NOT NULL,
    County          TEXT     NOT NULL,
    Status          TEXT     NOT NULL,
    CreatedDate     TEXT     NOT NULL,
    ModifiedDate    TEXT     NULL,
    IsDeleted       INTEGER  NOT NULL DEFAULT 0,
    DeletedDate     TEXT     NULL,
    DeletedBy       TEXT     NULL,
    CONSTRAINT CK_Providers_Status
        CHECK (Status IN ('Active','Inactive','Pending'))
);

CREATE INDEX IF NOT EXISTS IX_Providers_IsDeleted_ProviderName
    ON Providers (IsDeleted, ProviderName);

CREATE INDEX IF NOT EXISTS IX_Providers_IsDeleted_Status
    ON Providers (IsDeleted, Status);

-- ---------------------------------------------------------------------
-- Licenses
-- A License must belong to a Provider; cascade-delete is intentionally
-- disabled at the DB level - we cascade soft-deletes in application
-- code instead, so audit history is preserved.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Licenses (
    LicenseId       INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderId      INTEGER NOT NULL,
    LicenseNumber   TEXT     NOT NULL,
    LicenseStatus   TEXT     NOT NULL,
    ExpirationDate  TEXT     NOT NULL,
    CreatedDate     TEXT     NOT NULL,
    ModifiedDate    TEXT     NULL,
    IsDeleted       INTEGER  NOT NULL DEFAULT 0,
    DeletedDate     TEXT     NULL,
    DeletedBy       TEXT     NULL,
    CONSTRAINT FK_Licenses_Providers
        FOREIGN KEY (ProviderId) REFERENCES Providers (ProviderId)
        ON DELETE RESTRICT,
    CONSTRAINT CK_Licenses_LicenseStatus
        CHECK (LicenseStatus IN ('Active','Expired','Suspended'))
);

CREATE INDEX IF NOT EXISTS IX_Licenses_ProviderId_IsDeleted
    ON Licenses (ProviderId, IsDeleted);

CREATE INDEX IF NOT EXISTS IX_Licenses_ExpirationDate
    ON Licenses (ExpirationDate);

-- Partial unique index: a license number is unique per provider, but
-- only among non-deleted rows. Soft-deleted siblings are ignored so a
-- number can be reused after a deletion.
CREATE UNIQUE INDEX IF NOT EXISTS UX_Licenses_ProviderId_LicenseNumber_Active
    ON Licenses (ProviderId, LicenseNumber)
    WHERE IsDeleted = 0;
