using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace RxDBDotNet.Resolvers;

/// <summary>
/// Provides subscription resolvers for entities.
/// </summary>
/// <typeparam name="TEntity">The type of entity being replicated.</typeparam>
/// <typeparam name="TContext">The type of the DbContext.</typeparam>
public class SubscriptionResolvers<TEntity, TContext>
    where TEntity : class, IReplicatedEntity
    where TContext : DbContext
{
    private readonly TContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionResolvers{TEntity, TContext}"/> class.
    /// </summary>
    /// <param name="dbContext">The DbContext to be used for data access.</param>
    public SubscriptionResolvers(TContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Streams data updates for the entity type.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="PullBulk{TEntity}"/> object containing the latest updates.</returns>
    public async Task<PullBulk<TEntity>> GetStreamData()
    {
        var documents = await _dbContext.Set<TEntity>()
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.UpdatedAt)
            .ToListAsync();

        var lastUpdated = documents.Any() ? documents.Max(e => e.UpdatedAt) : DateTimeOffset.MinValue;

        return new PullBulk<TEntity>
        {
            Documents = documents,
            Checkpoint = new Checkpoint { Id = Guid.NewGuid(), UpdatedAt = lastUpdated }
        };
    }

    /// <summary>
    /// Subscribes to data updates for the entity type.
    /// </summary>
    /// <param name="eventReceiver">The event receiver for the subscription.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the subscription.</param>
    public async Task<ISourceStream<PullBulk<TEntity>>> Subscribe([Service] ITopicEventReceiver eventReceiver, CancellationToken cancellationToken)
    {
        var streamName = $"Stream_{typeof(TEntity).Name}";
        return await eventReceiver.SubscribeAsync<PullBulk<TEntity>>(streamName, cancellationToken);
    }
}
