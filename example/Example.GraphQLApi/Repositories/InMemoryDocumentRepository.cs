using System.Collections.Concurrent;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;

namespace Example.GraphQLApi.Repositories;

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
            // Check if the document has actually changed
            if (!AreDocumentsEqual(existingDocument, document))
            {
                // Update the document only if there are changes
                document.UpdatedAt = DateTimeOffset.UtcNow; // Update the timestamp
                _documents[document.Id] = document;
            }
            return Task.FromResult(document);
        }
        throw new InvalidOperationException("Document not found for update.");
    }

    /// <inheritdoc/>
    public Task MarkAsDeletedAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_documents.TryGetValue(id, out var document))
        {
            document.IsDeleted = true;
            _documents[id] = document;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public bool AreDocumentsEqual(TDocument doc1, TDocument doc2)
    {
        // For the in-memory implementation, we'll use reflection to compare all properties.
        // This approach is thorough but may be slower than necessary for some use cases.
        // In a production environment, you might want to implement a more optimized comparison.

        var type = typeof(TDocument);
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var value1 = property.GetValue(doc1);
            var value2 = property.GetValue(doc2);

            if (!Equals(value1, value2))
            {
                return false;
            }
        }

        return true;
    }
}
