using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

/// <summary>
///     Represents a GraphQL query for pulling documents.
/// </summary>
/// <typeparam name="TDocument">The type of the document to be pulled.</typeparam>
public class QueryExtension<TDocument>(ReplicationResolvers<TDocument, DbContext> resolvers) where TDocument : class, IReplicatedDocument
{
    /// <summary>
    ///     Pulls a set of documents from the server based on the specified checkpoint and limit.
    /// </summary>
    /// <param name="checkpoint">The checkpoint from which to start pulling documents.</param>
    /// <param name="limit">The maximum number of documents to pull.</param>
    /// <returns>A task representing the asynchronous operation, with a result of the pulled documents.</returns>
    public Task<PullDocumentsResult<TDocument>> PullDocuments(
        Checkpoint? checkpoint,
        int limit)
    {
        return resolvers.PullDocuments(checkpoint, limit);
    }
}
