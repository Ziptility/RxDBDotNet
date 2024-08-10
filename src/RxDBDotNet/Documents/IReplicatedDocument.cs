namespace RxDBDotNet.Documents;

/// <summary>
/// Represents the minimum requirements for a document to be replicated using RxDBDotNet.
/// This interface defines the core properties and behaviors necessary for document replication,
/// including support for topic-based event publishing.
/// </summary>
public interface IReplicatedDocument
{
    /// <summary>
    /// Gets the client-assigned identifier for this document.
    /// This property is used for client-side identification and replication purposes.
    /// </summary>
    /// <remarks>
    /// The Id should be unique within the collection of documents of the same type.
    /// It is typically generated on the client side to support offline-first scenarios.
    /// </remarks>
    Guid Id { get; }

    /// <summary>
    /// Gets or sets the timestamp of the last update to the document.
    /// </summary>
    /// <remarks>
    /// This property is crucial for conflict resolution and determining the most recent version of a document.
    /// It should be updated every time the document is modified.
    /// </remarks>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document has been marked as deleted.
    /// </summary>
    /// <remarks>
    /// When set to true, this property indicates a soft delete.
    /// Soft deletes allow for data recovery and maintain a history of deletions in the system.
    /// </remarks>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets an optional list of topics to publish events to when an instance is upserted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property enables fine-grained control over event publishing for document updates.
    /// When specified, events related to this document will only be published to the listed topics.
    /// </para>
    /// <para>
    /// Clients subscribing to document streams must specify one of these topic ids to receive events,
    /// providing a form of 'subscription filtering'. This mechanism ensures that subscribers only
    /// receive updates for the specific documents or document types they are interested in.
    /// </para>
    /// <para>
    /// If this property is null or an empty list, no subscription events will be published for this document.
    /// This can be used to optimize performance by limiting unnecessary event publishing and processing.
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
