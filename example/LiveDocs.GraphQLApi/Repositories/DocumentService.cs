using LiveDocs.GraphQLApi.Data;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Repositories;

/// <summary>
///     An implementation of IDocumentService using Entity Framework Core.
///     This class provides optimized database access for document operations required by the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
public class DocumentService<TDocument> : IDocumentService<TDocument> where TDocument : class, IReplicatedDocument
{
    private readonly LiveDocsDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly List<TDocument> _pendingEvents = [];

    public DocumentService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <inheritdoc />
    public IQueryable<TDocument> GetQueryableDocuments()
    {
        return _dbContext.Set<TDocument>();
    }

    /// <inheritdoc />
    public async Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken)
    {
        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<TDocument>()
            .FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        await _dbContext.Set<TDocument>()
            .AddAsync(document, cancellationToken);

        _pendingEvents.Add(document);

        return document;
    }

    /// <inheritdoc />
    public async Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existingDocument = await _dbContext.Set<TDocument>()
                                   .FindAsync([document.Id], cancellationToken)
                               ?? throw new InvalidOperationException($"Document with ID {document.Id} not found for update.");

        _dbContext.Entry(existingDocument)
            .CurrentValues.SetValues(document);
        existingDocument.UpdatedAt = DateTimeOffset.UtcNow;

        _pendingEvents.Add(existingDocument);

        return existingDocument;
    }

    /// <inheritdoc />
    public async Task<TDocument> MarkAsDeletedAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existingDocument = await _dbContext.Set<TDocument>()
                                   .FindAsync([document.Id], cancellationToken)
                               ?? throw new InvalidOperationException($"Document with ID {document.Id} not found for delete.");

        existingDocument.IsDeleted = true;

        existingDocument.UpdatedAt = DateTimeOffset.UtcNow;

        _pendingEvents.Add(existingDocument);

        return existingDocument;
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
        var entry1 = _dbContext.Entry(document1);
        var entry2 = _dbContext.Entry(document2);

        foreach (var property in entry1.Properties)
        {
            var name = property.Metadata.Name;
            if (name != nameof(IReplicatedDocument.UpdatedAt)) // Ignore UpdatedAt for comparison
            {
                var value1 = property.CurrentValue;
                var value2 = entry2.Property(name)
                    .CurrentValue;

                if (!Equals(value1, value2))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
