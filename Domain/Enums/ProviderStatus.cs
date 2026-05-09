namespace ProviderAssignmentStarter.Domain.Enums;

/// <summary>
/// Business / lifecycle state of a Provider.
/// </summary>
/// <remarks>
/// <para>
/// Persisted as TEXT in SQLite (via EF Core <c>HasConversion&lt;string&gt;()</c>)
/// rather than as integers. Three reasons:
/// <list type="number">
///   <item>The database stays human-readable when poked with the <c>sqlite3</c> CLI.</item>
///   <item>A <c>CHECK</c> constraint can guard the column directly using the same string literals.</item>
///   <item>Adding a new status doesn't risk renumbering an existing one and silently re-mapping historical rows.</item>
/// </list>
/// </para>
/// <para>
/// The integer values below are kept for source-level stability only — they
/// are not what hits the database.
/// </para>
/// </remarks>
public enum ProviderStatus
{
    /// <summary>Provider is operating normally and can hold valid licenses.</summary>
    Active = 1,

    /// <summary>Provider has been retired or paused and should not be relied upon.</summary>
    Inactive = 2,

    /// <summary>Provider has been registered but not yet activated.</summary>
    Pending = 3
}
