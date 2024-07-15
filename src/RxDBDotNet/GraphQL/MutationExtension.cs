using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Repositories;

namespace RxDBDotNet.GraphQL
{
    /// <summary>
    /// Represents a GraphQL mutation extension for pushing documents.
    /// This class implements the server-side logic for the 'push' operation in the RxDB replication protocol.
    /// </summary>
    /// <typeparam name="TDocument">The type of document being replicated, which must implement IReplicatedDocument.</typeparam>
    [ExtendObjectType("Mutation")]
    public class MutationExtension<TDocument> where TDocument : class, IReplicatedDocument
    {
        /// <summary>
        /// Pushes a set of documents to the server and handles any conflicts.
        /// This method implements the conflict resolution part of the RxDB replication protocol.
        /// </summary>
        /// <param name="documents">The list of documents to push, including their assumed master state.</param>
        /// <param name="repository">The document repository to be used for data access.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
        /// <returns>A task representing the asynchronous operation, with a result of any conflicting documents.</returns>
        public async Task<List<TDocument>> PushDocuments(
            List<DocumentPushRow<TDocument>> documents,
            [Service] IDocumentRepository<TDocument> repository,
            CancellationToken cancellationToken)
        {
            // Initialize lists to track conflicts, updates, and new documents
            // This separation allows us to handle each category appropriately
            var conflicts = new List<TDocument>();
            var updates = new List<TDocument>();
            var creates = new List<TDocument>();

            // Early return if no documents are provided
            // This is an optimization to avoid unnecessary processing
            if (documents?.Any() != true)
            {
                return conflicts;
            }

            // First pass: Detect conflicts and categorize documents
            // This aligns with the RxDB protocol's requirement to detect conflicts before applying changes
            foreach (var document in documents)
            {
                // Fetch the current state of the document from the repository
                var existing = await repository.GetDocumentByIdAsync(document.NewDocumentState.Id, cancellationToken);

                if (existing != null)
                {
                    // Document exists in the repository
                    if (document.AssumedMasterState == null ||
                        !AreDocumentsEqual(existing, document.AssumedMasterState))
                    {
                        // Conflict detected: The document has been modified since the client's last sync
                        // This implements the RxDB protocol's conflict detection mechanism
                        conflicts.Add(existing);
                    }
                    else
                    {
                        // No conflict: The document can be updated
                        updates.Add(document.NewDocumentState);
                    }
                }
                else if (document.AssumedMasterState == null)
                {
                    // Document doesn't exist and client doesn't assume it does: This is a new document
                    creates.Add(document.NewDocumentState);
                }
                else
                {
                    // Conflict: Client assumes a state for a non-existent document
                    // This handles edge cases where the client might be out of sync
                    conflicts.Add(document.AssumedMasterState);
                }
            }

            // Only proceed with updates if there are no conflicts
            // This ensures atomicity of operations as per the RxDB protocol
            if (conflicts.Count == 0)
            {
                try
                {
                    // Create new documents
                    foreach (var create in creates)
                    {
                        await repository.CreateDocumentAsync(create, cancellationToken);
                    }

                    // Update existing documents
                    foreach (var update in updates)
                    {
                        if (update.IsDeleted)
                        {
                            // Handle soft deletes as per RxDB protocol
                            await repository.MarkAsDeletedAsync(update.Id, cancellationToken);
                        }
                        else
                        {
                            await repository.UpdateDocumentAsync(update, cancellationToken);
                        }
                    }

                    // Commit all changes in a single transaction
                    // This ensures atomicity of the entire operation
                    await repository.SaveChangesAsync(cancellationToken);
                }
                catch (Exception)
                {
                    // If any exception occurs during the update process,
                    // we consider all documents as conflicting to ensure data integrity
                    // This is a conservative approach to maintain consistency
                    conflicts.AddRange(creates);
                    conflicts.AddRange(updates);
                }
            }

            // Return the list of conflicts
            // This allows the client to handle conflicts according to the RxDB protocol
            return conflicts;
        }

        /// <summary>
        /// Compares two documents to check if they are equal in terms of their content.
        /// </summary>
        /// <param name="doc1">The first document to compare.</param>
        /// <param name="doc2">The second document to compare.</param>
        /// <returns>True if the documents are equal, false otherwise.</returns>
        private static bool AreDocumentsEqual(TDocument doc1, TDocument doc2)
        {
            // This is a basic implementation. In a real-world scenario, you might want to
            // implement a more sophisticated comparison based on your document structure.
            return doc1.UpdatedAt == doc2.UpdatedAt && doc1.IsDeleted == doc2.IsDeleted;
        }
    }
}
