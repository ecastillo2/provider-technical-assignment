namespace ProviderAssignmentStarter.Domain.Common;

/// <summary>
/// Marker contract for any entity that participates in the application's
/// no-hard-delete policy.
/// </summary>
/// <remarks>
/// <para>
/// Soft-deleted rows remain in the database for audit and troubleshooting
/// but are excluded from every standard application query by the global
/// query filter configured on <c>AppDbContext</c>. Code that needs to see
/// deleted rows (audit listings, restore, debugging) must explicitly opt
/// in via <c>.IgnoreQueryFilters()</c> on the relevant query.
/// </para>
/// <para>
/// The three columns below are the persistent footprint of this contract:
/// <list type="bullet">
///   <item><see cref="IsDeleted"/> — the boolean flag the global filter checks.</item>
///   <item><see cref="DeletedDate"/> — the UTC timestamp set by the interceptor.</item>
///   <item><see cref="DeletedBy"/> — the principal who triggered the delete (currently hardcoded "system" because there is no auth in scope).</item>
/// </list>
/// </para>
/// <para>
/// Implementing this interface is the entry point for an entity to receive
/// the soft-delete behaviour. <c>BaseEntity</c> already implements it; any
/// new aggregate that extends <c>BaseEntity</c> gets the contract for free.
/// </para>
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>True once the row has been soft-deleted; false otherwise.</summary>
    bool IsDeleted { get; set; }

    /// <summary>UTC instant the row was soft-deleted, or null while live.</summary>
    DateTime? DeletedDate { get; set; }

    /// <summary>Identifier of the principal that performed the soft-delete, or null while live.</summary>
    string? DeletedBy { get; set; }
}
