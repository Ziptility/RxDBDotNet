using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

public class Subscription<TEntity> where TEntity : class, IReplicatedEntity
{
    [GraphQLName("streamEntity")]
    [Subscribe]
    public async Task<ISourceStream<PullBulk<TEntity>>> StreamEntity([Service] SubscriptionResolvers<TEntity, DbContext> resolvers, [Service] ITopicEventReceiver eventReceiver, CancellationToken cancellationToken)
        => await resolvers.Subscribe(eventReceiver, cancellationToken);
}
