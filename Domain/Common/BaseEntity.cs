namespace ProviderAssignmentStarter.Domain.Common;

/// <summary>
/// Base class for all persisted entities. Centralises audit and soft-delete
/// columns so every aggregate root participates in the same contract.
/// </summary>
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
    public string? DeletedBy { get; set; }
}
