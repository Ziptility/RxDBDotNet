using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Replication;
using RxDBDotNet.Repositories;

namespace RxDBDotNet.GraphQL
{
    /// <summary>
    /// Represents a GraphQL query extension for pulling documents.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document to be pulled.</typeparam>
    [ExtendObjectType("Query")]
    public class QueryExtension<TDocument> where TDocument : class, IReplicatedDocument
    {
        /// <summary>
        /// Pulls data from the backend based on the given checkpoint and limit.
        /// This method implements the 'checkpoint iteration' mode of the RxDB replication protocol.
        /// </summary>
        /// <param name="checkpoint">The last known checkpoint, or null if this is the initial pull.</param>
        /// <param name="limit">The maximum number of documents to return.</param>
        /// <param name="repository">The document repository to be used for data access.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a
        /// <see cref="PullDocumentsResult{TDocument}"/> object containing the pulled documents and the new checkpoint.
        /// </returns>
        public async Task<PullDocumentsResult<TDocument>> PullDocuments(
            Checkpoint? checkpoint,
            int limit,
            [Service] IDocumentRepository<TDocument> repository)
        {
            var documents = repository.GetDocuments(checkpoint?.UpdatedAt, limit);

            var filteredDocuments = await documents.FilterAndOrderDocuments(checkpoint).ToListAsync();

            var newCheckpoint = filteredDocuments.CreateCheckpoint(checkpoint);

            return new PullDocumentsResult<TDocument>
            {
                Documents = filteredDocuments,
                Checkpoint = newCheckpoint,
            };
        }
    }
}
