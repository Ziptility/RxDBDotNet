using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Repositories;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Services;

/// <summary>
///     An implementation of IDocumentService using Entity Framework Core.
///     This class provides optimized database access for document operations required by the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of replicated document being managed, which must implement IReplicatedDocument.</typeparam>
/// <typeparam name="TEntity">The type of entity in which the replicated document data is stored.</typeparam>
public abstract class DocumentService<TEntity, TDocument> : IDocumentService<TDocument>
    where TDocument : ReplicatedDocument
    where TEntity : ReplicatedEntity
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
    public IQueryable<TDocument> GetQueryableDocuments()
    {
        return _dbContext.Set<TEntity>().AsNoTracking().Select(ProjectToDocument());
    }

    /// <inheritdoc />
    public async Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken)
    {
        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await GetQueryableDocuments().Where(d => d.Id == id).SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        await _dbContext.Set<TEntity>()
            .AddAsync(Create(document), cancellationToken);

        _pendingEvents.Add(document);

        return document;
    }

    /// <inheritdoc />
    public async Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existingEntity = await _dbContext
                                 .Set<TEntity>()
                                 .Where(e => e.ReplicatedDocumentId == document.Id)
                                 .SingleOrDefaultAsync(cancellationToken)
                               ?? throw new InvalidOperationException($"Entity with a ReplicateDocumentId of {document.Id} not found for update.");

        Update(document, existingEntity);

        _pendingEvents.Add(document);

        return document;
    }

    /// <inheritdoc />
    public async Task<TDocument> MarkAsDeletedAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existingEntity = await _dbContext
                                 .Set<TEntity>()
                                 .Where(e => e.ReplicatedDocumentId == document.Id)
                                 .SingleOrDefaultAsync(cancellationToken)
                             ?? throw new InvalidOperationException($"Entity with a ReplicateDocumentId of {document.Id} not found for delete.");

        existingEntity.IsDeleted = true;
        existingEntity.UpdatedAt = document.UpdatedAt;

        _pendingEvents.Add(document);

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
    /// Projects the storage entity to its corresponding document.
    /// </summary>
    protected abstract Expression<Func<TEntity, TDocument>> ProjectToDocument();

    /// <summary>
    /// Updates an entity from the provided document.
    /// </summary>
    /// <param name="updatedDocument">The document from which to update the existing entity.</param>
    /// <param name="entityToUpdate">The existing entity to update from the document.</param>
    /// <returns>The updated entity.</returns>
    protected abstract TEntity Update(TDocument updatedDocument, TEntity entityToUpdate);

    /// <summary>
    /// Creates a new entity from the provided document.
    /// </summary>
    /// <param name="newDocument">The new document from which to crete the entity.</param>
    /// <returns>The newly created entity.</returns>
    protected abstract TEntity Create(TDocument newDocument);
}
