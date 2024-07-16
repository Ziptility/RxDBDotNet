using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
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

    private static IRequestExecutorBuilder ConfigureDocumentTypes<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder
            .AddCheckpointInputType<TDocument>()
            .AddDocumentPullBulkType<TDocument>()
            .AddDocumentPushRowInputType<TDocument>();
    }

    private static IRequestExecutorBuilder AddCheckpointInputType<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var checkpointInputTypeName = $"{typeof(TDocument).Name}InputCheckpoint";
        return builder.AddType(new InputObjectType<Checkpoint>(d =>
        {
            d.Name(checkpointInputTypeName);
            d.Description($"Input type for the checkpoint of {typeof(TDocument).Name} replication.");
            d.Field(f => f.UpdatedAt)
                .Type<DateTimeType>()
                .Description("The timestamp of the last update included in the synchronization batch.");
            d.Field(f => f.LastDocumentId)
                .Type<IdType>()
                .Description("The ID of the last document included in the synchronization batch.");
        }));
    }

    private static IRequestExecutorBuilder AddDocumentPullBulkType<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var pullBulkTypeName = $"{typeof(TDocument).Name}PullBulk";
        return builder.AddType(new ObjectType<DocumentPullBulk<TDocument>>(d =>
        {
            d.Name(pullBulkTypeName);
            d.Description($"Represents the result of a pull operation for {typeof(TDocument).Name} documents.");
            d.Field(f => f.Documents)
                .Description($"The list of {typeof(TDocument).Name} documents pulled from the server.");
            d.Field(f => f.Checkpoint)
                .Description("The new checkpoint after this pull operation.");
        }));
    }

    private static IRequestExecutorBuilder AddDocumentPushRowInputType<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var pushRowTypeName = $"{typeof(TDocument).Name}InputPushRow";
        return builder.AddType(new InputObjectType<DocumentPushRow<TDocument>>(d =>
        {
            d.Name(pushRowTypeName);
            d.Description($"Input type for pushing {typeof(TDocument).Name} documents to the server.");
            d.Field(f => f.AssumedMasterState)
                .Type<InputObjectType<TDocument>>()
                .Description("The assumed state of the document on the server before the push.");
            d.Field(f => f.NewDocumentState)
                .Type<NonNullType<InputObjectType<TDocument>>>()
                .Description("The new state of the document being pushed.");
        }));
    }

    private static IRequestExecutorBuilder ConfigureDocumentQueries<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var documentTypeName = typeof(TDocument).Name;
        var pullDocumentsName = $"pull{documentTypeName}";
        var checkpointInputTypeName = $"{documentTypeName}InputCheckpoint";

        return builder.AddType(new ObjectType(d =>
        {
            d.Name("Query");
            d.Field(pullDocumentsName)
                .Type<NonNullType<ObjectType<DocumentPullBulk<TDocument>>>>()
                .Argument("checkpoint", a =>
                {
                    a.Type(checkpointInputTypeName);
                    a.Description($"The last known checkpoint for {documentTypeName} replication.");
                })
                .Argument("limit", a =>
                {
                    a.Type<NonNullType<IntType>>();
                    a.Description($"The maximum number of {documentTypeName} documents to return.");
                })
                .Description($"Pulls {documentTypeName} documents from the server based on the given checkpoint and limit.")
                .Resolve(ResolvePullDocuments<TDocument>);
        }));
    }

    private static Task<DocumentPullBulk<TDocument>> ResolvePullDocuments<TDocument>(IResolverContext context)
        where TDocument : class, IReplicatedDocument
    {
        var queryResolver = context.Resolver<QueryResolver<TDocument>>();
        var checkpoint = context.ArgumentValue<Checkpoint?>("checkpoint");
        var limit = context.ArgumentValue<int>("limit");
        var repository = context.Service<IDocumentRepository<TDocument>>();
        var cancellationToken = context.RequestAborted;

        return queryResolver.PullDocumentsAsync(checkpoint, limit, repository, cancellationToken);
    }

    private static IRequestExecutorBuilder ConfigureDocumentMutations<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var documentTypeName = typeof(TDocument).Name;
        var pushDocumentsName = $"push{documentTypeName}";
        var pushRowArgName = $"{char.ToLowerInvariant(documentTypeName[0])}{documentTypeName[1..]}PushRow";

        return builder.AddType(new ObjectType(d =>
        {
            d.Name("Mutation");
            d.Field(pushDocumentsName)
                .Type<NonNullType<ListType<NonNullType<ObjectType<TDocument>>>>>()
                .Argument(pushRowArgName, a =>
                {
                    a.Type<ListType<InputObjectType<DocumentPushRow<TDocument>>>>();
                    a.Description($"The list of {documentTypeName} documents to push to the server.");
                })
                .Description($"Pushes {documentTypeName} documents to the server and handles any conflicts.")
                .Resolve(context => ResolvePushDocuments<TDocument>(context, pushRowArgName));
        }));
    }

    private static async Task<IReadOnlyList<TDocument>> ResolvePushDocuments<TDocument>(IResolverContext context, string pushRowArgName)
        where TDocument : class, IReplicatedDocument
    {
        var mutation = context.Resolver<MutationResolver<TDocument>>();
        var documents = context.ArgumentValue<List<DocumentPushRow<TDocument>?>?>(pushRowArgName);
        var repository = context.Service<IDocumentRepository<TDocument>>();
        var cancellationToken = context.RequestAborted;

        return await mutation.PushDocumentsAsync(documents, repository, cancellationToken);
    }
}
