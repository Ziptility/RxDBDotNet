using System.Collections.Concurrent;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;

namespace Example.GraphQLApi.Repositories
{
    /// <summary>
    /// An in-memory implementation of IDocumentRepository that simulates a database for testing and prototyping purposes.
    /// This implementation is thread-safe and supports the full IDocumentRepository interface.
    /// </summary>
    /// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
    public class InMemoryDocumentRepository<TDocument> : IDocumentRepository<TDocument> where TDocument : class, IReplicatedDocument
    {
        private readonly ConcurrentDictionary<Guid, TDocument> _documents = new();

        /// <inheritdoc/>
        public IQueryable<TDocument> GetQueryableDocuments()
        {
            return _documents.Values.AsQueryable();
        }

        /// <inheritdoc/>
        public Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken)
        {
            // Since this is an in-memory implementation, we can execute the query immediately
            return Task.FromResult(query.ToList());
        }

        /// <inheritdoc/>
        public Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            _documents.TryGetValue(id, out var document);
            return Task.FromResult(document);
        }

        /// <inheritdoc/>
        public Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken)
        {
            if (_documents.TryAdd(document.Id, document))
            {
                return Task.FromResult(document);
            }
            throw new InvalidOperationException("Document with the same ID already exists.");
        }

        /// <inheritdoc/>
        public Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken)
        {
            if (_documents.TryGetValue(document.Id, out var existingDocument))
            {
                _documents[document.Id] = document;
                return Task.FromResult(document);
            }
            throw new InvalidOperationException("Document not found for update.");
        }

        /// <inheritdoc/>
        public Task MarkAsDeletedAsync(Guid id, CancellationToken cancellationToken)
        {
            if (_documents.TryGetValue(id, out var document))
            {
                // Create a new instance with the IsDeleted flag set to true
                var deletedDocument = (TDocument)Activator.CreateInstance(typeof(TDocument), [
                    document.Id,
                    document.UpdatedAt,
                    true, // Set IsDeleted to true
                ])!;

                _documents[id] = deletedDocument;
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            // No operation needed for in-memory repository as changes are immediate
            return Task.CompletedTask;
        }
    }
}
