namespace RxDBDotNet;

/// <summary>
/// Represents a row of data being pushed, including its assumed master state.
/// </summary>
/// <typeparam name="TEntity">The type of entity being replicated.</typeparam>
public class PushRow<TEntity> where TEntity : class, IReplicatedEntity
{
    /// <summary>
    /// The assumed state of the document on the master before the push.
    /// </summary>
    public required TEntity? AssumedMasterState { get; set; }

    /// <summary>
    /// The new state of the document being pushed.
    /// </summary>
    public required TEntity NewDocumentState { get; set; }
}
