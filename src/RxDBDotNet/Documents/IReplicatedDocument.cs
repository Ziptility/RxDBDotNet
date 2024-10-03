namespace RxDBDotNet.Documents;

/// <summary>
/// Represents the minimum requirements for a document to be replicated using RxDBDotNet.
/// This interface defines the core properties and behaviors necessary for document replication,
/// including support for topic-based event publishing.
/// </summary>
public interface IReplicatedDocument
{
    /// <summary>
    /// The client-assigned identifier for this document.
    /// This property is used for client-side identification and replication purposes.
    /// </summary>
    /// <remarks>
    /// The Id must be unique within the collection of documents of the same type.
    /// It is typically generated on the client side to support offline-first scenarios.
    /// </remarks>
    Guid Id { get; }

    /// <summary>
    /// The timestamp of the last update to the document.
    /// </summary>
    /// <remarks>
    /// This property is crucial for conflict resolution and determining the most recent version of a document.
    /// It should be updated every time the document is modified.
    /// The server will always overwrite this value with its own timestamp to ensure security and consistency.
    /// </remarks>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// A value indicating whether the document has been marked as deleted.
    /// </summary>
    /// <remarks>
    /// When set to true, this property indicates a soft delete.
    /// Soft deletes allow for data recovery and maintain a history of deletions in the system.
    /// </remarks>
    bool IsDeleted { get; }

    /// <summary>
    /// An optional list of topics to publish events to when an instance is upserted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property enables fine-grained control over event publishing for document updates.
    /// When specified, events related to this document will only be published to the listed topics.
    /// </para>
    /// <para>
    /// Clients subscribing to document streams can specify one of these topic ids to receive events,
    /// providing a form of 'subscription filtering'. This mechanism ensures that subscribers can
    /// receive updates for the specific documents they are interested in.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// public class MyDocument : IReplicatedDocument
    /// {
    ///     public Guid Id { get; set; }
    ///     public DateTimeOffset UpdatedAt { get; set; }
    ///     public bool IsDeleted { get; set; }
    ///     public List&lt;string&gt;? Topics { get; set; } = new List&lt;string&gt; { "important-updates", "audit-log" };
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    List<string>? Topics { get; }
}
