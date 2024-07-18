using Microsoft.Extensions.Logging;
using RxDBDotNet.Documents;
using RxDBDotNet.Services;

namespace RxDBDotNet.Repositories;

/// <summary>
/// Provides a base implementation for document repositories with event publishing capabilities.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
/// <remarks>
/// Initializes a new instance of the BaseDocumentRepository class.
/// </remarks>
/// <param name="eventPublisher">The event publisher used to publish document change events.</param>
/// <param name="logger">The logger used for logging operations and errors.</param>
public abstract class BaseDocumentRepository<TDocument>(IEventPublisher eventPublisher, ILogger<BaseDocumentRepository<TDocument>> logger) : IDocumentRepository<TDocument>
    where TDocument : class, IReplicatedDocument
{
    private readonly List<TDocument> _pendingEvents = [];

    /// <inheritdoc/>
    public abstract IQueryable<TDocument> GetQueryableDocuments();

    /// <inheritdoc/>
    public abstract Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public async Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        var createdDocument = await CreateDocumentInternalAsync(document, cancellationToken).ConfigureAwait(false);
        _pendingEvents.Add(createdDocument);
        return createdDocument;
    }

    /// <inheritdoc/>
    public async Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        var updatedDocument = await UpdateDocumentInternalAsync(document, cancellationToken).ConfigureAwait(false);
        _pendingEvents.Add(updatedDocument);
        return updatedDocument;
    }

    /// <inheritdoc/>
    public async Task MarkAsDeletedAsync(Guid id, CancellationToken cancellationToken)
    {
        var deletedDocument = await MarkAsDeletedInternalAsync(id, cancellationToken).ConfigureAwait(false);
        if (deletedDocument != null)
        {
            _pendingEvents.Add(deletedDocument);
        }
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesInternalAsync(cancellationToken).ConfigureAwait(false);

        foreach (var document in _pendingEvents)
        {
            try
            {
                await eventPublisher.PublishDocumentChangedEventAsync(document, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish document changed event for document with ID {DocumentId}", document.Id);
            }
        }

        _pendingEvents.Clear();
    }

    /// <inheritdoc/>
    public abstract bool AreDocumentsEqual(TDocument doc1, TDocument doc2);

    /// <summary>
    /// Internal method to create a document in the repository.
    /// </summary>
    /// <param name="document">The document to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>The created document.</returns>
    protected abstract Task<TDocument> CreateDocumentInternalAsync(TDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// Internal method to update a document in the repository.
    /// </summary>
    /// <param name="document">The document to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>The updated document.</returns>
    protected abstract Task<TDocument> UpdateDocumentInternalAsync(TDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// Internal method to mark a document as deleted in the repository.
    /// </summary>
    /// <param name="id">The ID of the document to mark as deleted.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>The deleted document, or null if the document was not found.</returns>
    protected abstract Task<TDocument?> MarkAsDeletedInternalAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Internal method to save changes to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task SaveChangesInternalAsync(CancellationToken cancellationToken);
}
