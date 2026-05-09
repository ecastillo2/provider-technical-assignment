namespace ProviderAssignmentStarter.Domain.Common;

/// <summary>
/// Base class for every persisted aggregate root in the domain.
/// </summary>
/// <remarks>
/// <para>
/// Centralises the audit and soft-delete columns so every aggregate
/// participates in the same behavioural contract. New entities only need
/// to inherit from this type to receive:
/// <list type="bullet">
///   <item>Audit timestamps stamped automatically by <c>AuditableInterceptor</c>.</item>
///   <item>Soft-delete columns flipped automatically by <c>SoftDeleteInterceptor</c>.</item>
///   <item>Exclusion from standard queries via the global query filter on <c>AppDbContext</c>.</item>
/// </list>
/// </para>
/// <para>
/// This is an abstract class rather than just two interfaces because keeping
/// the audit / soft-delete columns physically inline on every entity makes
/// the schema self-contained and avoids a polymorphic table-inheritance
/// strategy that SQLite handles poorly. The trade-off — a small amount of
/// column duplication across tables — is intentional.
/// </para>
/// </remarks>
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    // -- Audit (stamped by AuditableInterceptor) --
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // -- Soft-delete (flipped by SoftDeleteInterceptor) --
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
    public string? DeletedBy { get; set; }
}
