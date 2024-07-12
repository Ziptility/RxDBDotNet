using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

public class Mutation<TEntity> where TEntity : class, IReplicatedEntity
{
    [GraphQLName("pushEntity")]
    public Task<List<TEntity>> PushEntity(List<PushRow<TEntity>> rows, [Service] ReplicationResolvers<TEntity, DbContext> resolvers)
        => resolvers.PushData(rows);
}
