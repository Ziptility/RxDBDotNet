namespace RxDBDotNet;

/// <summary>
/// Represents the minimum requirements for an entity to be replicated using this extension.
/// </summary>
public interface IReplicatedEntity
{
    /// <summary>
    /// The unique identifier for the entity.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// The timestamp of the last update to the entity.
    /// </summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Indicates whether the entity has been marked as deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}
