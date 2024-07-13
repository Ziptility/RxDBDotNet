namespace RxDBDotNet.Models;

/// <summary>
///     Represents a checkpoint in the replication process.
///     Checkpoints are used to track the state of synchronization
///     between the client and server, ensuring that only new or updated
///     documents are synchronized.
/// </summary>
public class Checkpoint
{
    /// <summary>
    ///     <para>
    ///         Gets or sets the ID of the last document included in the synchronization batch.
    ///         This ID is used to ensure that synchronization can accurately resume
    ///         if it is interrupted, by providing a unique identifier for the last processed document.
    ///     </para>
    ///     <para>
    ///         If the value is null, it indicates that there are no documents to synchronize,
    ///         and the client should treat this as a starting point or an indication that no previous checkpoint exists.
    ///     </para>
    /// </summary>
    public required Guid? LastDocumentId { get; init; }

    /// <summary>
    ///     <para>
    ///         Gets or sets the timestamp of the last update included in the synchronization batch.
    ///         This timestamp helps in filtering out documents that have already been synchronized,
    ///         ensuring that only new or updated documents are processed during synchronization.
    ///     </para>
    ///     <para>
    ///         If the value is null, it indicates that there are no updates to synchronize,
    ///         and the client should treat this as a starting point or an indication that no previous checkpoint exists.
    ///     </para>
    /// </summary>
    public required DateTimeOffset? UpdatedAt { get; init; }
}
