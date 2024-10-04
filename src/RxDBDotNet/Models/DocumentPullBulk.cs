// src\RxDBDotNet\Models\DocumentPullBulk.cs
using RxDBDotNet.Documents;

namespace RxDBDotNet.Models;

/// <summary>
///     Represents the result of a replication pull operation.
/// </summary>
/// <typeparam name="TDocument">The type of the documents being pulled.</typeparam>
public sealed class DocumentPullBulk<TDocument> where TDocument : IReplicatedDocument
{
    /// <summary>
    ///     The list of pulled documents.
    /// </summary>
    public required List<TDocument> Documents { get; init; }

    /// <summary>
    ///     The new checkpoint after this pull operation.
    ///     This checkpoint indicates the latest state of synchronization and should be stored
    ///     by the client to resume synchronization from this point in the future.
    /// </summary>
    public required Checkpoint Checkpoint { get; init; }
}
