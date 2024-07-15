using System.Collections.Concurrent;
using RxDBDotNet.Documents;
using RxDBDotNet.Exceptions;
using RxDBDotNet.Repositories;

namespace Example.GraphQLApi.Repositories;

/// <summary>
/// An enhanced in-memory implementation of IDocumentRepository that simulates a database for testing and prototyping purposes.
/// This implementation is thread-safe, supports the full IDocumentRepository interface, and follows best practices.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryDocumentRepository{TDocument}"/> class.
/// </remarks>
/// <param name="logger">The logger to use for logging operations and errors.</param>
public class InMemoryDocumentRepository<TDocument>(ILogger<InMemoryDocumentRepository<TDocument>> logger) : IDocumentRepository<TDocument>
    where TDocument : class, IReplicatedDocument
{
    private readonly ConcurrentDictionary<Guid, TDocument> _documents = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <inheritdoc/>
    public IQueryable<TDocument> GetQueryableDocuments()
    {
        _lock.EnterReadLock();
        try
        {
            return _documents.Values.AsQueryable();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc/>
    public Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken)
    {
        // Since this is in-memory, we can execute the query immediately
        return Task.FromResult(query.ToList());
    }

    /// <inheritdoc/>
    public Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _lock.EnterReadLock();
        try
        {
            _documents.TryGetValue(id, out var document);
            return Task.FromResult(document);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc/>
    public Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_documents.TryAdd(document.Id, document))
            {
                logger.LogInformation("Document created successfully. ID: {DocumentId}", document.Id);
                return Task.FromResult(document);
            }

            logger.LogError("Document with ID {DocumentId} already exists.", document.Id);
            throw new ConcurrencyException($"Document with ID {document.Id} already exists.");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_documents.TryGetValue(document.Id, out var existingDocument))
            {
                if (!AreDocumentsEqual(existingDocument, document))
                {
                    document.UpdatedAt = DateTimeOffset.UtcNow;
                    _documents[document.Id] = document;
                    logger.LogInformation("Document updated successfully. ID: {DocumentId}", document.Id);
                }
                return Task.FromResult(document);
            }

            logger.LogError("Document with ID {DocumentId} not found for update.", document.Id);
            throw new InvalidOperationException($"Document with ID {document.Id} not found for update.");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public Task MarkAsDeletedAsync(Guid id, CancellationToken cancellationToken)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_documents.TryGetValue(id, out var document))
            {
                document.IsDeleted = true;
                document.UpdatedAt = DateTimeOffset.UtcNow;
                _documents[id] = document;
                logger.LogInformation("Document marked as deleted successfully. ID: {DocumentId}", id);
            }
            else
            {
                logger.LogWarning("Attempted to mark non-existent document as deleted. ID: {DocumentId}", id);
            }
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // For in-memory repository, changes are saved immediately, so this is a no-op
        logger.LogInformation("SaveChangesAsync called. No action required for in-memory repository.");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public bool AreDocumentsEqual(TDocument doc1, TDocument doc2)
    {
        var type = typeof(TDocument);
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            if (property.Name == nameof(IReplicatedDocument.UpdatedAt))
            {
                continue; // Skip UpdatedAt for comparison
            }

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
