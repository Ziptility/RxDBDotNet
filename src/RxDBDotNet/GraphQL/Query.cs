using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

public class Query<TEntity> where TEntity : class, IReplicatedEntity
{
    [GraphQLName("pullEntity")]
    public Task<PullBulk<TEntity>> PullEntity(Checkpoint checkpoint, int limit, [Service] ReplicationResolvers<TEntity, DbContext> resolvers)
        => resolvers.PullData(checkpoint, limit);
}
