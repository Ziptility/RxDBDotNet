namespace RxDBDotNet;

/// <summary>
/// Represents the result of a bulk pull operation for replication.
/// </summary>
/// <typeparam name="TEntity">The type of the documents being pulled.</typeparam>
public class PullBulk<TEntity> where TEntity : class, IReplicatedEntity
{
    /// <summary>
    /// The list of pulled documents.
    /// </summary>
    public required List<TEntity> Documents { get; init; }

    /// <summary>
    /// The new checkpoint after this pull operation.
    /// </summary>
    public required Checkpoint Checkpoint { get; init; }
}
