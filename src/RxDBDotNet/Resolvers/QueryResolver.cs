using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Repositories;

namespace RxDBDotNet.Resolvers;

/// <summary>
/// Represents a GraphQL query resolver for pulling documents.
/// This class implements the server-side logic for the 'pull' operation in the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of the document to be pulled, which must implement IReplicatedDocument.</typeparam>
public sealed class QueryResolver<TDocument> where TDocument : class, IReplicatedDocument
{
    /// <summary>
    /// Pulls documents from the backend based on the given checkpoint and limit.
    /// This method implements the 'checkpoint iteration' mode of the RxDB replication protocol.
    /// </summary>
    /// <param name="checkpoint">The last known checkpoint, or null if this is the initial pull.</param>
    /// <param name="limit">The maximum number of documents to return.</param>
    /// <param name="repository">The document repository to be used for data access.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="DocumentPullBulk{TDocument}"/> object containing the pulled documents and the new checkpoint.
    /// </returns>
    /// <remarks>
    /// This method is implemented to efficiently retrieve documents that have been updated since the last checkpoint.
    /// It uses a combination of the UpdatedAt timestamp and the document ID to ensure consistent and complete results,
    /// even when multiple documents have the same UpdatedAt value.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The method obtains its parameters from the DI context")]
#pragma warning disable MA0051 // Method is too long. This is because of the extensive documentation, which is acceptable in this case for maintainability.
    internal async Task<DocumentPullBulk<TDocument>> PullDocumentsAsync(
        Checkpoint? checkpoint,
        int limit,
        IDocumentRepository<TDocument> repository,
        CancellationToken cancellationToken)
    {
        var query = repository.GetQueryableDocuments();

        // If a checkpoint is provided, we use it to filter the documents.
        // This is crucial for the efficiency of the replication process, as it allows
        // us to only retrieve documents that have changed since the last synchronization.
        if (checkpoint != null)
        {
            // We set default values for UpdatedAt and LastDocumentId to ensure consistent query behavior.
            // This simplifies our query logic and avoids potential null reference exceptions.

            // DateTimeOffset.MinValue is used as the default for UpdatedAt because any real document
            // will have a greater UpdatedAt value. This ensures we get all documents if no real
            // checkpoint UpdatedAt is provided.
            var checkpointUpdatedAt = checkpoint.UpdatedAt ?? DateTimeOffset.MinValue;

            // Guid.Empty is used as the default for LastDocumentId. While it's a valid GUID,
            // it's extremely unlikely to match a real document ID in practice, especially
            // if using a GUID generation strategy that produces sequential or random GUIDs.
            var checkpointLastDocumentId = checkpoint.LastDocumentId ?? Guid.Empty;

            query = query.Where(d =>
                // First, we check if the document was updated after the checkpoint
                d.UpdatedAt > checkpointUpdatedAt ||
                // If the UpdatedAt is the same as the checkpoint, we need to check the ID
                // to ensure we don't miss documents updated at the exact same time
                (d.UpdatedAt == checkpointUpdatedAt &&
                 // We exclude the document with the exact checkpoint ID to avoid duplication
                 d.Id != checkpointLastDocumentId &&
                 // We compare string representations of GUIDs because EF Core can translate
                 // string comparisons to SQL, unlike direct GUID comparisons
                 d.Id.ToString().CompareTo(checkpointLastDocumentId.ToString()) > 0));
        }

        // We order the results to ensure consistent pagination across multiple pulls
        var orderedQuery = query
            .OrderBy(d => d.UpdatedAt)
            // We order by ID as a secondary sort to handle documents with the same UpdatedAt
            .ThenBy(d => d.Id.ToString())
            .Take(limit);

        var documents = await repository.ExecuteQueryAsync(orderedQuery, cancellationToken).ConfigureAwait(false);

        // If no documents are returned, we return an empty result with a null checkpoint
        // This signals to the client that there are no more documents to pull
        if (documents.Count == 0)
        {
            return new DocumentPullBulk<TDocument>
            {
                Documents = [],
                Checkpoint = new Checkpoint
                {
                    LastDocumentId = null,
                    UpdatedAt = null,
                },
            };
        }

        // The new checkpoint is based on the last document in the returned set
        // This ensures that the next pull will start from where this one left off
        var newCheckpoint = new Checkpoint
        {
            UpdatedAt = documents[^1].UpdatedAt,
            LastDocumentId = documents[^1].Id,
        };

        return new DocumentPullBulk<TDocument>
        {
            Documents = documents,
            Checkpoint = newCheckpoint,
        };
    }
}
