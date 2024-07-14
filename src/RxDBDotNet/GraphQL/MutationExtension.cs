using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Repositories;

namespace RxDBDotNet.GraphQL
{
    /// <summary>
    /// Represents a GraphQL mutation extension for pushing documents.
    /// </summary>
    /// <typeparam name="TDocument">The type of document being replicated.</typeparam>
    [ExtendObjectType("Mutation")]
    public class MutationExtension<TDocument> where TDocument : class, IReplicatedDocument
    {
        /// <summary>
        /// Pushes a set of documents to the server and handles any conflicts.
        /// </summary>
        /// <param name="documents">The list of documents to push, including their assumed master state.</param>
        /// <param name="repository">The document repository to be used for data access.</param>
        /// <returns>A task representing the asynchronous operation, with a result of any conflicting documents.</returns>
        public async Task<List<TDocument>> PushDocuments(
            List<PushDocumentRequest<TDocument>> documents,
            [Service] IDocumentRepository<TDocument> repository)
        {
            var conflicts = new List<TDocument>();

            if (documents?.Any() != true)
            {
                return conflicts;
            }

            foreach (var document in documents)
            {
                var existing = await repository.GetDocumentByIdAsync(document.NewDocumentState.Id);

                if (existing != null)
                {
                    if (document.AssumedMasterState == null
                        || existing.UpdatedAt != document.AssumedMasterState.UpdatedAt)
                    {
                        conflicts.Add(existing);
                    }
                    else
                    {
                        await repository.UpdateDocumentAsync(document.NewDocumentState);
                    }
                }
                else
                {
                    await repository.CreateDocumentAsync(document.NewDocumentState);
                }
            }

            await repository.SaveChangesAsync();

            return conflicts;
        }
    }
}
