namespace RxDBDotNet.Documents;

/// <summary>
///     Represents the minimum requirements for a document to be replicated using RxDbDotNet.
/// </summary>
public interface IReplicatedDocument
{
    /// <summary>
    /// Gets the client-assigned identifier for this document.
    /// This property is used for client-side identification and replication purposes.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     The timestamp of the last update to the document.
    /// </summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    ///     Indicates whether the document has been marked as deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}
