using RxDBDotNet.Documents;
using RxDBDotNet.Security;

namespace RxDBDotNet.Extensions;

/// <summary>
/// Provides configuration options for replicating documents in RxDBDotNet.
/// </summary>
/// <typeparam name="TDocument">
/// The type of document to be replicated, which must implement <see cref="IReplicatedDocument"/>.
/// </typeparam>
public sealed class ReplicationOptions<TDocument>
    where TDocument : class, IReplicatedDocument
{
    /// <summary>
    /// Gets the security options for the replication of documents.
    /// </summary>
    /// <value>
    /// An instance of <see cref="SecurityOptions{TDocument}"/> that provides configuration for security policies
    /// related to the replication of documents.
    /// </value>
    public SecurityOptions<TDocument> Security { get; } = new();
}
