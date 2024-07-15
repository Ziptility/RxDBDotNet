using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Repositories;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.Extensions;

/// <summary>
/// Provides extension methods for configuring GraphQL services with RxDBDotNet replication support.
/// This class integrates the RxDB replication protocol with Hot Chocolate GraphQL server,
/// enabling seamless real-time data synchronization between clients and the server.
/// </summary>
public static class GraphQLServiceCollectionExtensions
{
    /// <summary>
    /// Adds replication support for a specific document type to the GraphQL schema.
    /// This method configures all necessary types, queries, and mutations for the RxDB replication protocol.
    /// The names are generated such that they match the expectations of the RxDB GraphQL extension.
    /// </summary>
    /// <typeparam name="TDocument">The type of document to support, which must implement IReplicatedDocument.</typeparam>
    /// <param name="builder">The IRequestExecutorBuilder to configure.</param>
    /// <returns>The configured IRequestExecutorBuilder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method should be called for each document type that needs to be replicated.
    /// It's typically used in the ConfigureServices method of the Startup class or in the
    /// service configuration of a minimal API setup.
    /// </para>
    /// <para>
    /// Usage example:
    /// <code>
    /// services.AddGraphQLServer()
    ///     .AddReplicationSupport&lt;MyDocument&gt;()
    ///     .AddReplicationSupport&lt;AnotherDocument&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    public static IRequestExecutorBuilder AddReplicationSupport<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder
            .AddResolver<QueryResolver<TDocument>>()
            .AddResolver<MutationResolver<TDocument>>()
            .ConfigureDocumentTypes<TDocument>()
            .ConfigureDocumentQueries<TDocument>()
            .ConfigureDocumentMutations<TDocument>();
    }

    /// <summary>
    /// Configures the GraphQL types required for document replication.
    /// </summary>
    /// <typeparam name="TDocument">The document type to configure.</typeparam>
    /// <param name="builder">The IRequestExecutorBuilder to configure.</param>
    /// <returns>The configured IRequestExecutorBuilder.</returns>
    private static IRequestExecutorBuilder ConfigureDocumentTypes<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        // Generate type names based on the document type to ensure uniqueness
        var documentTypeName = typeof(TDocument).Name;
        var checkpointInputTypeName = $"{documentTypeName}InputCheckpoint";
        var pullBulkTypeName = $"{documentTypeName}PullBulk";
        var pushRowTypeName = $"{documentTypeName}InputPushRow";

        return builder
            // Configure the Checkpoint input type
            .AddType(new InputObjectType<Checkpoint>(d =>
            {
                d.Name(checkpointInputTypeName);
                d.Field(f => f.UpdatedAt).Type<DateTimeType>();
                d.Field(f => f.LastDocumentId).Type<IdType>();
            }))
            // Configure the DocumentPullBulk type for bulk document retrieval
            .AddType(new ObjectType<DocumentPullBulk<TDocument>>(d => d.Name(pullBulkTypeName)))
            // Configure the DocumentPushRow input type for pushing document changes
            .AddType(new InputObjectType<DocumentPushRow<TDocument>>(d =>
            {
                d.Name(pushRowTypeName);
                d.Field(f => f.AssumedMasterState).Type<InputObjectType<TDocument>>();
                d.Field(f => f.NewDocumentState).Type<NonNullType<InputObjectType<TDocument>>>();
            }));
    }

    /// <summary>
    /// Configures the GraphQL queries required for document replication.
    /// </summary>
    /// <typeparam name="TDocument">The document type to configure queries for.</typeparam>
    /// <param name="builder">The IRequestExecutorBuilder to configure.</param>
    /// <returns>The configured IRequestExecutorBuilder.</returns>
    private static IRequestExecutorBuilder ConfigureDocumentQueries<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var documentTypeName = typeof(TDocument).Name;
        var pullDocumentsName = $"pull{documentTypeName}";
        var checkpointInputTypeName = $"{documentTypeName}InputCheckpoint";

        return builder.AddType(new ObjectType(d =>
        {
            d.Name("Query");
            // Configure the 'pull' query
            const string checkpointArgumentName = "checkpoint";
            const string limitArgumentName = "limit";
            d.Field(pullDocumentsName)
                .Type<NonNullType<ObjectType<DocumentPullBulk<TDocument>>>>()
                .Argument(checkpointArgumentName, a => a.Type(checkpointInputTypeName))
                .Argument(limitArgumentName, a => a.Type<NonNullType<IntType>>())
                .Resolve(async context =>
                {
                    var queryResolver = context.Resolver<QueryResolver<TDocument>>();
                    var checkpoint = context.ArgumentValue<Checkpoint?>(checkpointArgumentName);
                    var limit = context.ArgumentValue<int>(limitArgumentName);
                    var repository = context.Service<IDocumentRepository<TDocument>>();
                    var cancellationToken = context.RequestAborted;

                    return await queryResolver.PullDocumentsAsync(checkpoint, limit, repository, cancellationToken);
                });
        }));
    }

    /// <summary>
    /// Configures the GraphQL mutations required for document replication.
    /// </summary>
    /// <typeparam name="TDocument">The document type to configure mutations for.</typeparam>
    /// <param name="builder">The IRequestExecutorBuilder to configure.</param>
    /// <returns>The configured IRequestExecutorBuilder.</returns>
    private static IRequestExecutorBuilder ConfigureDocumentMutations<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var documentTypeName = typeof(TDocument).Name;
        var pushDocumentsName = $"push{documentTypeName}";
        var pushRowArgName = $"{char.ToLowerInvariant(documentTypeName[0])}{documentTypeName.Substring(1)}PushRow";

        return builder.AddType(new ObjectType(d =>
        {
            d.Name("Mutation");
            // Configure the 'push' mutation
            d.Field(pushDocumentsName)
                .Type<NonNullType<ListType<NonNullType<ObjectType<TDocument>>>>>()
                .Argument(pushRowArgName, a => a.Type<ListType<InputObjectType<DocumentPushRow<TDocument>>>>())
                .Resolve(async context =>
                {
                    var mutation = context.Resolver<MutationResolver<TDocument>>();
                    var documents = context.ArgumentValue<List<DocumentPushRow<TDocument>?>?>(pushRowArgName);
                    var repository = context.Service<IDocumentRepository<TDocument>>();
                    var cancellationToken = context.RequestAborted;

                    return await mutation.PushDocumentsAsync(documents, repository, cancellationToken);
                });
        }));
    }
}
