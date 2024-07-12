using Microsoft.EntityFrameworkCore;

namespace RxDB.NET.Resolvers;

/// <summary>
/// Provides data replication resolvers for entities.
/// </summary>
/// <typeparam name="TEntity">The type of entity being replicated.</typeparam>
/// <typeparam name="TContext">The type of the DbContext.</typeparam>
public class ReplicationResolvers<TEntity, TContext>
    where TEntity : class, IReplicatedEntity
    where TContext : DbContext
{
    private readonly TContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicationResolvers{TEntity, TContext}"/> class.
    /// </summary>
    /// <param name="dbContext">The DbContext to be used for data access.</param>
    public ReplicationResolvers(TContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Pulls data from the backend based on the given checkpoint and limit.
    /// </summary>
    /// <param name="checkpoint">The last known checkpoint.</param>
    /// <param name="limit">The maximum number of documents to return.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="PullBulk{TEntity}"/> object containing the pulled documents and the new checkpoint.</returns>
    public async Task<PullBulk<TEntity>> PullData(Checkpoint checkpoint, int limit)
    {
        var documents = await _dbContext.Set<TEntity>()
            .Where(e => e.UpdatedAt > checkpoint.UpdatedAt)
            .OrderBy(e => e.UpdatedAt)
            .Take(limit)
            .ToListAsync();

        var lastUpdated = documents.Any() ? documents.Max(e => e.UpdatedAt) : checkpoint.UpdatedAt;

        return new PullBulk<TEntity>
        {
            Documents = documents,
            Checkpoint = new Checkpoint { Id = Guid.NewGuid(), UpdatedAt = lastUpdated }
        };
    }

    /// <summary>
    /// Pushes data to the backend and handles any conflicts.
    /// </summary>
    /// <param name="rows">The list of documents to push, including their assumed master state.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of conflicting documents, if any.</returns>
    public async Task<List<TEntity>> PushData(List<PushRow<TEntity>> rows)
    {
        var conflicts = new List<TEntity>();

        foreach (var row in rows)
        {
            var existing = await _dbContext.Set<TEntity>().FindAsync(row.NewDocumentState.Id);
            if (existing != null)
            {
                if (row.AssumedMasterState == null || existing.UpdatedAt != row.AssumedMasterState.UpdatedAt)
                {
                    conflicts.Add(existing);
                }
                else
                {
                    _dbContext.Entry(existing).CurrentValues.SetValues(row.NewDocumentState);
                }
            }
            else
            {
                _dbContext.Set<TEntity>().Add(row.NewDocumentState);
            }
        }

        await _dbContext.SaveChangesAsync();

        return conflicts;
    }
}