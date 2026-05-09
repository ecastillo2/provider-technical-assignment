namespace ProviderAssignmentStarter.Domain.Common;

/// <summary>
/// Marks an entity that participates in the soft-delete contract.
/// Soft-deleted rows remain in the database for audit but are excluded
/// from standard application queries by the global query filter.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedDate { get; set; }
    string? DeletedBy { get; set; }
}
