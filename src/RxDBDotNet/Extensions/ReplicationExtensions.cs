using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Types;

namespace RxDBDotNet.Extensions;

/// <summary>
/// Provides extension methods for adding RxDBDotNet replication support to a Hot Chocolate GraphQL server.
/// </summary>
public static class ReplicationExtensions
{
    /// <summary>
    /// Adds RxDBDotNet replication support for a specific entity type to the GraphQL server.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to support for replication.</typeparam>
    /// <param name="builder">The GraphQL server builder.</param>
    /// <returns>The updated GraphQL server builder.</returns>
    public static IRequestExecutorBuilder AddRxDBDotNetReplication<TEntity>(this IRequestExecutorBuilder builder)
        where TEntity : class, IReplicatedEntity
    {
        return builder
            .AddType<ReplicationExtension<TEntity>>()
            .AddType<RxDBSubscriptionType<TEntity>>()
            .AddType<EntityType<TEntity>>()
            .AddType<PullBulkType<TEntity>>()
            .AddType<PushRowInputType<TEntity>>()
            .AddType<EntityInputType<TEntity>>()
            .AddType<CheckpointInputType>()
            .AddType<HeadersInputType>();
    }
}
