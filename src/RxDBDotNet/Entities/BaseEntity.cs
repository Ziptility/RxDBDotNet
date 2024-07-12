namespace RxDBDotNet.Entities;

/// <summary>
/// Represents the minimum requirements for an entity to be replicated using this extension.
/// </summary>
public abstract class BaseEntity : IReplicatedEntity
{
    /// <summary>
    /// The unique identifier for the entity.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// The timestamp of the last update to the entity.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Indicates whether the entity has been marked as deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}