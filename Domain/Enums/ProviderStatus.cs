namespace ProviderAssignmentStarter.Domain.Enums;

/// <summary>
/// Business / lifecycle state of a Provider. Persisted as text in SQLite
/// so the database remains human-readable and CHECK-constrained.
/// </summary>
public enum ProviderStatus
{
    Active = 1,
    Inactive = 2,
    Pending = 3
}
