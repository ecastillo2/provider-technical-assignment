namespace ProviderAssignmentStarter.Domain.Common;

/// <summary>
/// Marker contract for an entity whose insert/update timestamps are stamped
/// automatically by <c>AuditableInterceptor</c> at <c>SaveChanges</c> time.
/// </summary>
/// <remarks>
/// <para>
/// Callers should never set these fields directly. The interceptor:
/// <list type="bullet">
///   <item>Writes <see cref="CreatedDate"/> on first insert (state == Added).</item>
///   <item>Writes <see cref="ModifiedDate"/> on every subsequent update (state == Modified).</item>
///   <item>Marks <see cref="CreatedDate"/> as not-modified during updates so it cannot be overwritten by accident.</item>
/// </list>
/// </para>
/// <para>
/// Both timestamps are stored as UTC. Views render them in local time.
/// </para>
/// </remarks>
public interface IAuditable
{
    /// <summary>UTC instant the row was first created. Set once and never changed.</summary>
    DateTime CreatedDate { get; set; }

    /// <summary>UTC instant of the most recent update, or null if never updated since insert.</summary>
    DateTime? ModifiedDate { get; set; }
}
