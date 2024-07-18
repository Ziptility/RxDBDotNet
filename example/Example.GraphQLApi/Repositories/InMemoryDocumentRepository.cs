using System.Collections.Concurrent;
using Example.GraphQLApi.Models;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;
using RxDBDotNet.Services;

namespace Example.GraphQLApi.Repositories;

/// <summary>
/// An in-memory implementation of IDocumentRepository that simulates a database for testing and prototyping purposes.
/// This implementation is thread-safe, supports the full IDocumentRepository interface, and follows best practices.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
/// <remarks>
/// Initializes a new instance of the InMemoryDocumentRepository class.
/// </remarks>
/// <param name="eventPublisher">The event publisher used to publish document change events.</param>
/// <param name="logger">The logger to use for logging operations and errors.</param>
public sealed class InMemoryDocumentRepository<TDocument>(IEventPublisher eventPublisher, ILogger<InMemoryDocumentRepository<TDocument>> logger) : BaseDocumentRepository<TDocument>(eventPublisher, logger), IDisposable
    where TDocument : class, IReplicatedDocument
{
    private readonly ConcurrentDictionary<Guid, TDocument> _documents = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;

    /// <inheritdoc/>
    public override IQueryable<TDocument> GetQueryableDocuments()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
    public override Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.FromResult(query.ToList());
    }

    /// <inheritdoc/>
    public override Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
    protected override Task<TDocument> CreateDocumentInternalAsync(TDocument document, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(document);
        
        _lock.EnterWriteLock();
        try
        {
            if (_documents.TryAdd(document.Id, document))
            {
                return Task.FromResult(document);
            }

            throw new ConcurrencyException($"Document with ID {document.Id} already exists.");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    protected override Task<TDocument> UpdateDocumentInternalAsync(TDocument document, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(document);
        
        _lock.EnterWriteLock();
        try
        {
            if (_documents.TryGetValue(document.Id, out var existingDocument))
            {
                if (!AreDocumentsEqual(existingDocument, document))
                {
                    document.UpdatedAt = DateTimeOffset.UtcNow;
                    _documents[document.Id] = document;
                }
                return Task.FromResult(document);
            }

            throw new InvalidOperationException($"Document with ID {document.Id} not found for update.");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    protected override Task<TDocument?> MarkAsDeletedInternalAsync(Guid id, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _lock.EnterWriteLock();
        try
        {
            if (_documents.TryGetValue(id, out var document))
            {
                document.IsDeleted = true;
                document.UpdatedAt = DateTimeOffset.UtcNow;
                _documents[id] = document;
                return Task.FromResult<TDocument?>(document);
            }
            return Task.FromResult<TDocument?>(null);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    protected override Task SaveChangesInternalAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // For in-memory repository, changes are saved immediately, so this is a no-op
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override bool AreDocumentsEqual(TDocument doc1, TDocument doc2)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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

    /// <summary>
    /// Disposes the resources used by the InMemoryDocumentRepository.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _lock.Dispose();
            _disposed = true;
        }
    }
}
