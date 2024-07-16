using Example.GraphQLApi.Models;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;

namespace Example.GraphQLApi.Repositories;

/// <summary>
/// An enhanced implementation of IDocumentRepository using Entity Framework Core.
/// This class provides optimized database access for document operations required by the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use for data access.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="EfDocumentRepository{TDocument, TContext}"/> class.
/// </remarks>
/// <param name="context">The DbContext to use for data access.</param>
/// <param name="logger">The logger to use for logging operations and errors.</param>
public class EfDocumentRepository<TDocument, TContext>(TContext context, ILogger<EfDocumentRepository<TDocument, TContext>> logger) : IDocumentRepository<TDocument>
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
        return context.Set<TDocument>().FindAsync(new object[] { id }, cancellationToken).AsTask();
    }

    /// <inheritdoc/>
    public async Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        context.Set<TDocument>().Add(document);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Document created successfully. ID: {DocumentId}", document.Id);
            return document;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error creating document. ID: {DocumentId}", document.Id);
            throw new ConcurrencyException($"Error creating document with ID {document.Id}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken)
    {
        var existingDocument = await context.Set<TDocument>().FindAsync(new object[] { document.Id }, cancellationToken)
                               ?? throw new InvalidOperationException($"Document with ID {document.Id} not found for update.");

        if (!AreDocumentsEqual(existingDocument, document))
        {
            context.Entry(existingDocument).CurrentValues.SetValues(document);
            existingDocument.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Document updated successfully. ID: {DocumentId}", document.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Concurrency conflict occurred while updating document. ID: {DocumentId}", document.Id);
                throw new ConcurrencyException($"Concurrency conflict occurred while updating document with ID {document.Id}", document.Id);
            }
        }

        return existingDocument;
    }

    /// <inheritdoc/>
    public async Task MarkAsDeletedAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await context.Set<TDocument>().FindAsync(new object[] { id }, cancellationToken);
        if (document != null)
        {
            document.IsDeleted = true;
            document.UpdatedAt = DateTimeOffset.UtcNow;
            context.Update(document);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Document marked as deleted successfully. ID: {DocumentId}", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Concurrency conflict occurred while marking document as deleted. ID: {DocumentId}", id);
                throw new ConcurrencyException($"Concurrency conflict occurred while marking document with ID {id} as deleted", id);
            }
        }
        else
        {
            logger.LogWarning("Attempted to mark non-existent document as deleted. ID: {DocumentId}", id);
        }
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Changes saved successfully.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency conflict occurred while saving changes.");
            throw new ConcurrencyException("Concurrency conflict occurred while saving changes", ex);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error occurred while saving changes.");
            throw new ConcurrencyException("Error occurred while saving changes", ex);
        }
    }

    /// <inheritdoc/>
    public bool AreDocumentsEqual(TDocument doc1, TDocument doc2)
    {
        var entry1 = context.Entry(doc1);
        var entry2 = context.Entry(doc2);

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

    /// <summary>
    /// Performs a batch update operation on multiple documents.
    /// </summary>
    /// <param name="documents">The list of documents to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task BatchUpdateAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken)
    {
        foreach (var document in documents)
        {
            context.Entry(document).State = EntityState.Modified;
            document.UpdatedAt = DateTimeOffset.UtcNow;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Batch update completed successfully.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogError(ex, "Concurrency conflict occurred during batch update.");
            throw new ConcurrencyException("Concurrency conflict occurred during batch update", ex);
        }
    }
}
