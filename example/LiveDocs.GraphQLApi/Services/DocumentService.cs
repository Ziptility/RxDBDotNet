// example/LiveDocs.GraphQLApi/Services/DocumentService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Services;

/// <summary>
///     An implementation of IDocumentService using Entity Framework Core.
///     This class provides optimized database access for document operations required by the RxDB replication protocol.
/// </summary>
/// <typeparam name="TEntity">The type of entity in which the replicated document data is stored.</typeparam>
/// <typeparam name="TDocument">The type of replicated document being managed, which must implement ReplicatedDocument.</typeparam>
public abstract class DocumentService<TEntity, TDocument> : IDocumentService<TDocument>
    where TEntity : ReplicatedEntity
    where TDocument : class, IReplicatedDocument
{
    private readonly LiveDocsDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly List<TDocument> _pendingEvents = [];

    protected DocumentService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <inheritdoc />
    public abstract IQueryable<TDocument> GetQueryableDocuments();

    /// <inheritdoc />
    public Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetQueryableDocuments().Where(d => d.Id == id).SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var newEntity = await CreateAsync(document, cancellationToken);

        await _dbContext.Set<TEntity>()
            .AddAsync(newEntity, cancellationToken);

        // The document may have been updated during the update process, so we need to map it
        // so that the latest version gets propagated to the client
        var newDocument = MapToDocument(newEntity);

        _pendingEvents.Add(newDocument);

        return document;
    }

    /// <summary>
    /// Retrieves an entity by its associated document ID.
    /// </summary>
    /// <param name="documentId">The unique identifier of the document.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity associated with the specified document ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="documentId"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no entity is found for the specified <paramref name="documentId"/>.</exception>
    protected abstract Task<TEntity> GetEntityByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken);

    /// <inheritdoc />
    public async Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existingEntity = await GetEntityByDocumentIdAsync(document.Id, cancellationToken);

        existingEntity = Update(document, existingEntity);

        // The document may have been updated during the update process, so we need to map it
        // so that the latest version gets propagated to the client
        var updatedDocument = MapToDocument(existingEntity);

        _pendingEvents.Add(updatedDocument);

        return document;
    }

    /// <inheritdoc />
    public async Task<TDocument> MarkAsDeletedAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existingEntity = await GetEntityByDocumentIdAsync(document.Id, cancellationToken);

        existingEntity.IsDeleted = true;
        existingEntity.UpdatedAt = document.UpdatedAt;

        // The document is updated during the update process, so we need to map it
        // so that the latest version gets propagated to the client
        var deletedDocument = MapToDocument(existingEntity);

        _pendingEvents.Add(deletedDocument);

        return document;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var doc in _pendingEvents)
        {
            await _eventPublisher.PublishDocumentChangedEventAsync(doc, cancellationToken);
        }

        _pendingEvents.Clear();
    }

    /// <inheritdoc />
    public bool AreDocumentsEqual(TDocument document1, TDocument document2)
    {
        ArgumentNullException.ThrowIfNull(document1);
        ArgumentNullException.ThrowIfNull(document2);

        return document1.Equals(document2);
    }

    /// <summary>
    /// Maps the storage entity to its corresponding document.
    /// </summary>
    /// <param name="entityToMap">The entity to map to a document.</param>
    /// <returns>The mapped document.</returns>
    protected abstract TDocument MapToDocument(TEntity entityToMap);

    /// <summary>
    /// Updates an entity from the provided document.
    /// </summary>
    /// <param name="updatedDocument">The document from which to update the existing entity.</param>
    /// <param name="entityToUpdate">The existing entity to update from the document.</param>
    /// <returns>The updated entity.</returns>
    protected abstract TEntity Update(TDocument updatedDocument, TEntity entityToUpdate);

    /// <summary>
    /// Asynchronously creates a new entity from the provided document.
    /// </summary>
    /// <param name="newDocument">The new document from which to create the entity.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the newly created entity.</returns>
    protected abstract Task<TEntity> CreateAsync(TDocument newDocument, CancellationToken cancellationToken);
}
