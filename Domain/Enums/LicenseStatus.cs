namespace ProviderAssignmentStarter.Domain.Enums;

/// <summary>
/// Lifecycle state of a License. Note: a license can be Expired by its
/// ExpirationDate even if its status is still recorded as Active — the
/// "active provider with expired licenses" scenario the spec calls out.
/// </summary>
public enum LicenseStatus
{
    Active = 1,
    Expired = 2,
    Suspended = 3
}
