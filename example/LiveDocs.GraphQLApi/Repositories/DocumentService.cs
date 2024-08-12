using LiveDocs.GraphQLApi.Models;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Repositories;

/// <summary>
/// An implementation of IDocumentService using Entity Framework Core.
/// This class provides optimized database access for document operations required by the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use for data access.</typeparam>
/// <remarks>
/// Initializes a new instance of the EfDocumentService class.
/// </remarks>
/// <param name="context">The DbContext to use for data access.</param>
/// <param name="eventPublisher">The event publisher used to publish document change events.</param>
/// <param name="logger">The logger to use for logging operations and errors.</param>
public class DocumentService<TDocument, TContext>(
    TContext context,
    IEventPublisher eventPublisher,
    ILogger<DocumentService<TDocument, TContext>> logger) : BaseDocumentService<TDocument>(eventPublisher, logger)
    where TDocument : class, IReplicatedDocument
    where TContext : DbContext
{
    private readonly TContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public override IQueryable<TDocument> GetQueryableDocuments()
    {
        return _context.Set<TDocument>().AsNoTracking();
    }

    /// <inheritdoc/>
    public override Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.Set<TDocument>().FindAsync([id], cancellationToken).AsTask();
    }

    /// <inheritdoc/>
    protected override async Task<TDocument> CreateDocumentInternalAsync(TDocument document, CancellationToken cancellationToken)
    {
        await _context.Set<TDocument>().AddAsync(document, cancellationToken).ConfigureAwait(false);
        return document;
    }

    /// <inheritdoc/>
    protected override async Task<TDocument> UpdateDocumentInternalAsync(TDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        
        var existingDocument = await _context.Set<TDocument>().FindAsync([document.Id], cancellationToken).ConfigureAwait(false)
                               ?? throw new InvalidOperationException($"Document with ID {document.Id} not found for update.");

        if (!AreDocumentsEqual(existingDocument, document))
        {
            _context.Entry(existingDocument).CurrentValues.SetValues(document);
            existingDocument.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return existingDocument;
    }

    /// <inheritdoc/>
    protected override async Task<TDocument?> MarkAsDeletedInternalAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _context.Set<TDocument>().FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (document != null)
        {
            document.IsDeleted = true;
            document.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Update(document);
        }

        return document;
    }

    /// <inheritdoc/>
    protected override async Task SaveChangesInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Concurrency conflict occurred while saving changes", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new ConcurrencyException("Error occurred while saving changes", ex);
        }
    }

    /// <inheritdoc/>
    public override bool AreDocumentsEqual(TDocument doc1, TDocument doc2)
    {
        var entry1 = _context.Entry(doc1);
        var entry2 = _context.Entry(doc2);

        foreach (var property in entry1.Properties)
        {
            var name = property.Metadata.Name;
            if (name != nameof(IReplicatedDocument.UpdatedAt)) // Ignore UpdatedAt for comparison
            {
                var value1 = property.CurrentValue;
                var value2 = entry2.Property(name).CurrentValue;

                if (!Equals(value1, value2))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
