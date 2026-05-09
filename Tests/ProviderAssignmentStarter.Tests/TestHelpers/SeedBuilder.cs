using ProviderAssignmentStarter.Domain.Entities;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.Tests.TestHelpers;

/// <summary>
/// Tiny convenience helpers for building deterministic test data. Each
/// test seeds only what it needs - shared fixtures hide the dependencies
/// that make tests brittle.
/// </summary>
internal static class SeedBuilder
{
    public static Provider NewProvider(
        string name = "Test Provider",
        string county = "Fulton",
        ProviderStatus status = ProviderStatus.Active,
        params License[] licenses) => new()
        {
            ProviderName = name,
            County       = county,
            Status       = status,
            Licenses     = licenses.ToList()
        };

    public static License NewLicense(
        string number = "TEST-001",
        LicenseStatus status = LicenseStatus.Active,
        DateTime? expiration = null) => new()
        {
            LicenseNumber  = number,
            LicenseStatus  = status,
            ExpirationDate = expiration ?? DateTime.UtcNow.Date.AddYears(1)
        };
}
