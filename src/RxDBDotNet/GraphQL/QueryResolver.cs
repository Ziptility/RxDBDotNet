using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Repositories;

namespace RxDBDotNet.GraphQL;

/// <summary>
/// Represents a GraphQL query resolver for pulling documents.
/// This class implements the server-side logic for the 'pull' operation in the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of the document to be pulled, which must implement IReplicatedDocument.</typeparam>
public class QueryResolver<TDocument> where TDocument : class, IReplicatedDocument
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
    public async Task<DocumentPullBulk<TDocument>> PullDocuments(
        Checkpoint? checkpoint,
        int limit,
        [Service] IDocumentRepository<TDocument> repository,
        CancellationToken cancellationToken)
    {
        var query = repository.GetQueryableDocuments();

        if (checkpoint != null)
        {
            query = query.Where(d =>
                d.UpdatedAt > checkpoint.UpdatedAt ||
                (d.UpdatedAt == checkpoint.UpdatedAt && d.Id.CompareTo(checkpoint.LastDocumentId) > 0));
        }

        var orderedQuery = query
            .OrderBy(d => d.UpdatedAt)
            .ThenBy(d => d.Id)
            .Take(limit);

        var documents = await repository.ExecuteQueryAsync(orderedQuery, cancellationToken);

        if (!documents.Any())
        {
            // Return an empty array when there are no more documents to pull
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
