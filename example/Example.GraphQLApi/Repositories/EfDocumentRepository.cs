using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;

namespace Example.GraphQLApi.Repositories;

/// <summary>
/// An implementation of IDocumentRepository using Entity Framework Core.
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
    public IQueryable<TDocument> GetDocuments()
    {
        return context.Set<TDocument>().AsQueryable();
    }

    /// <inheritdoc/>
    public async Task<TDocument?> GetDocumentByIdAsync(Guid id)
    {
        return await context.Set<TDocument>().FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<TDocument> CreateDocumentAsync(TDocument document)
    {
        var entry = await context.Set<TDocument>().AddAsync(document);
        return entry.Entity;
    }

    /// <inheritdoc/>
    public Task<TDocument> UpdateDocumentAsync(TDocument document)
    {
        var entry = context.Set<TDocument>().Update(document);
        return Task.FromResult(entry.Entity);
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
