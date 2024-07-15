using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Exceptions;
using RxDBDotNet.Repositories;

namespace Example.GraphQLApi.Repositories;

/// <summary>
/// An implementation of IDocumentRepository using Entity Framework Core.
/// This class provides database access for document operations required by the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use for data access.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="EfDocumentRepository{TDocument, TContext}"/> class.
/// </remarks>
/// <param name="context">The DbContext to use for data access.</param>
public class EfDocumentRepository<TDocument, TContext>(TContext context) : IDocumentRepository<TDocument>
    where TDocument : class, IReplicatedDocument
    where TContext : DbContext
{
    /// <inheritdoc/>
    public IQueryable<TDocument> GetQueryableDocuments()
    {
        return context.Set<TDocument>().AsNoTracking();
    }

    /// <inheritdoc/>
    public Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Set<TDocument>().FindAsync([id], cancellationToken).AsTask();
    }

    /// <inheritdoc/>
    public async Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        context.Set<TDocument>().Add(document);
        await context.SaveChangesAsync(cancellationToken);
        return document;
    }

    /// <inheritdoc/>
    public async Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        var existingDocument = await context.Set<TDocument>().FindAsync([document.Id], cancellationToken)
                               ?? throw new InvalidOperationException($"Document with ID {document.Id} not found for update.");
        
        if (!AreDocumentsEqual(existingDocument, document))
        {
            // Update only if there are changes
            context.Entry(existingDocument).CurrentValues.SetValues(document);
            existingDocument.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle concurrency conflict
                throw new ConcurrencyException($"Concurrency conflict occurred while updating document with ID {document.Id}");
            }
        }

        return existingDocument;
    }

    /// <inheritdoc/>
    public async Task MarkAsDeletedAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await context.Set<TDocument>().FindAsync([id], cancellationToken);
        if (document != null)
        {
            document.IsDeleted = true;
            context.Update(document);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public bool AreDocumentsEqual(TDocument doc1, TDocument doc2)
    {
        // This implementation uses EF Core's ChangeTracker to compare entities.
        // It's efficient but may not catch all differences if the entities are not being tracked.

        // Ensure both entities are being tracked
        var entry1 = context.Entry(doc1);
        var entry2 = context.Entry(doc2);

        if (entry1.State == EntityState.Detached)
        {
            entry1.State = EntityState.Unchanged;
        }

        if (entry2.State == EntityState.Detached)
        {
            entry2.State = EntityState.Unchanged;
        }

        // Compare all properties
        var properties = entry1.Properties;
        foreach (var property in properties)
        {
            var value1 = property.CurrentValue;
            var value2 = entry2.Property(property.Metadata.Name).CurrentValue;

            if (!Equals(value1, value2))
            {
                return false;
            }
        }

        return true;
    }
}
