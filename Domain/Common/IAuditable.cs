namespace ProviderAssignmentStarter.Domain.Common;

/// <summary>
/// Marks an entity whose audit timestamps are set automatically by the
/// AuditableInterceptor on insert/update.
/// </summary>
public interface IAuditable
{
    DateTime CreatedDate { get; set; }
    DateTime? ModifiedDate { get; set; }
}
