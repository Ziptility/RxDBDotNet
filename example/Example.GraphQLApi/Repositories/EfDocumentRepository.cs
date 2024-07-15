using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Repositories;

namespace Example.GraphQLApi.Repositories
{
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
            return context.Set<TDocument>().FindAsync(new object[] { id }, cancellationToken).AsTask();
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
            context.Entry(document).State = EntityState.Modified;
            await context.SaveChangesAsync(cancellationToken);
            return document;
        }

        /// <inheritdoc/>
        public async Task MarkAsDeletedAsync(Guid id, CancellationToken cancellationToken)
        {
            var document = await context.Set<TDocument>().FindAsync(new object[] { id }, cancellationToken);
            if (document != null)
            {
                // Assuming TDocument has a settable IsDeleted property
                // If not, you may need to create a new instance or use a different approach
                ((dynamic)document).IsDeleted = true;
                context.Update(document);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return context.SaveChangesAsync(cancellationToken);
        }
    }
}
