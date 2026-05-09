namespace ProviderAssignmentStarter.Domain.Enums;

/// <summary>
/// Lifecycle state of a License.
/// </summary>
/// <remarks>
/// <para>
/// A license can be considered "expired" by either of two independent
/// signals:
/// <list type="bullet">
///   <item>An explicit <see cref="Expired"/> status set by an administrator.</item>
///   <item>An <c>ExpirationDate</c> that has passed, regardless of recorded status.</item>
/// </list>
/// This double-channel encoding is the source of the assignment's flagship
/// scenario: an Active provider can have an Active-status license whose
/// expiration date has silently lapsed. The dashboard's "active providers
/// with expired licenses" widget hunts exactly this case.
/// </para>
/// <para>
/// Persisted as TEXT for the same reasons as <see cref="ProviderStatus"/>.
/// </para>
/// </remarks>
public enum LicenseStatus
{
    /// <summary>License is currently in good standing, status-wise.</summary>
    Active = 1,

    /// <summary>License has been explicitly marked as expired.</summary>
    Expired = 2,

    /// <summary>License is paused / under review and cannot be used until lifted.</summary>
    Suspended = 3
}
