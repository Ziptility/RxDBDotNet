using System.Collections.Concurrent;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;

namespace Example.GraphQLApi.Repositories
{
    /// <summary>
    /// An in-memory implementation of IDocumentRepository that simulates a database for testing and prototyping purposes.
    /// </summary>
    /// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
    public class InMemoryDocumentRepository<TDocument> : IDocumentRepository<TDocument> where TDocument : class, IReplicatedDocument
    {
        private readonly ConcurrentDictionary<Guid, TDocument> _documents = new();

        /// <inheritdoc/>
        public IQueryable<TDocument> GetDocuments()
        {
            return _documents.Values.AsQueryable();
        }

        /// <inheritdoc/>
        public Task<TDocument?> GetDocumentByIdAsync(Guid id)
        {
            _documents.TryGetValue(id, out var document);
            
            return Task.FromResult(document);
        }

        /// <inheritdoc/>
        public Task<TDocument> CreateDocumentAsync(TDocument document)
        {
            if (!_documents.TryAdd(document.Id, document))
            {
                throw new InvalidOperationException($"Document with ID {document.Id} already exists.");
            }
            return Task.FromResult(document);
        }

        /// <inheritdoc/>
        public Task<TDocument> UpdateDocumentAsync(TDocument document)
        {
            if (!_documents.TryUpdate(document.Id, document, _documents[document.Id]))
            {
                throw new InvalidOperationException($"Failed to update document with ID {document.Id}.");
            }
            return Task.FromResult(document);
        }

        //// <inheritdoc/>
        public Task SaveChangesAsync()
        {
            // No operation needed for in-memory repository
            return Task.CompletedTask;
        }
    }
}
