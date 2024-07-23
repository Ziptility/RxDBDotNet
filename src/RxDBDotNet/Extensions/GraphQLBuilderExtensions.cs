using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Repositories;
using RxDBDotNet.Resolvers;
using RxDBDotNet.Services;

namespace RxDBDotNet.Extensions;

/// <summary>
/// Provides extension methods for configuring GraphQL services with RxDBDotNet replication support.
/// This class integrates the RxDB replication protocol with Hot Chocolate GraphQL server,
/// enabling seamless real-time data synchronization between clients and the server.
/// </summary>
public static class GraphQLBuilderExtensions
{
    /// <summary>
    /// Adds replication support for RxDBDotNet to the GraphQL schema.
    /// This method configures all necessary services and types for the RxDB replication protocol.
    /// </summary>
    /// <param name="builder">The IRequestExecutorBuilder to configure.</param>
    /// <returns>The configured IRequestExecutorBuilder for method chaining.</returns>
    /// <remarks>
    /// This method should be called once before adding support for specific document types.
    /// It registers core services like IEventPublisher that are shared across all document types.
    /// </remarks>
    public static IRequestExecutorBuilder AddReplicationServer(this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IEventPublisher, DefaultEventPublisher>();

        // Add projections and filtering support in the correct order
        builder.AddProjections()
            .AddFiltering();

        // Ensure Query, Mutation, and Subscription types exist
        EnsureRootTypesExist(builder);

        builder.AddMutationConventions();

        return builder.InitializeOnStartup();
    }

    /// <summary>
    /// Adds replication support for a specific document type to the GraphQL schema.
    /// This method configures all necessary types, queries, mutations, and subscriptions for the RxDB replication protocol.
    /// </summary>
    /// <typeparam name="TDocument">The type of document to support, which must implement IReplicatedDocument.</typeparam>
    /// <param name="builder">The HotChocolate IRequestExecutorBuilder to configure.</param>
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
    ///     .AddReplicationServer()
    ///     .AddReplicatedDocument&lt;MyDocument&gt;()
    ///     .AddReplicatedDocument&lt;AnotherDocument&gt;();
    /// </code>
    /// </para>
    /// </remarks>
    public static IRequestExecutorBuilder AddReplicatedDocument<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder
            .AddResolver<QueryResolver<TDocument>>()
            .AddResolver<MutationResolver<TDocument>>()
            .AddResolver<SubscriptionResolver<TDocument>>()
            .ConfigureDocumentTypes<TDocument>()
            .ConfigureDocumentQueries<TDocument>()
            .ConfigureDocumentMutations<TDocument>()
            .ConfigureDocumentSubscriptions<TDocument>();
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
            d.Name(checkpointInputTypeName)
                .Description($"Input type for the checkpoint of {typeof(TDocument).Name} replication.");
            d.Field(f => f.UpdatedAt).Type<DateTimeType>().Description("The timestamp of the last update included in the synchronization batch.");
            d.Field(f => f.LastDocumentId).Type<IdType>().Description("The ID of the last document included in the synchronization batch.");
        }));
    }

    private static IRequestExecutorBuilder AddDocumentPullBulkType<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var pullBulkTypeName = $"{typeof(TDocument).Name}PullBulk";
        return builder.AddType(new ObjectType<DocumentPullBulk<TDocument>>(descriptor =>
        {
            descriptor.Name(pullBulkTypeName)
                .Description($"Represents the result of a pull operation for {typeof(TDocument).Name} documents.");
            descriptor.Field(f => f.Documents)
                .Description($"The list of {typeof(TDocument).Name} documents pulled from the server.");
            descriptor.Field(f => f.Checkpoint)
                .Description("The new checkpoint after this pull operation.");
        }));
    }

    private static IRequestExecutorBuilder AddDocumentPushRowInputType<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var pushRowTypeName = $"{typeof(TDocument).Name}InputPushRow";
        return builder.AddType(new InputObjectType<DocumentPushRow<TDocument>>(descriptor =>
        {
            descriptor.Name(pushRowTypeName)
                .Description($"Input type for pushing {typeof(TDocument).Name} documents to the server.");
            descriptor.Field(f => f.AssumedMasterState)
                .Type<InputObjectType<TDocument>>()
                .Description("The assumed state of the document on the server before the push.");
            descriptor.Field(f => f.NewDocumentState)
                .Type<NonNullType<InputObjectType<TDocument>>>()
                .Description("The new state of the document being pushed.");
        }));
    }

    private static IRequestExecutorBuilder ConfigureDocumentQueries<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder.AddTypeExtension<QueryExtension<TDocument>>();
    }

#pragma warning disable CA1812
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class QueryExtension<TDocument> : ObjectTypeExtension
#pragma warning restore CA1812
        where TDocument : class, IReplicatedDocument
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            var documentTypeName = typeof(TDocument).Name;
            var pullDocumentsName = $"pull{documentTypeName}";
            var checkpointInputTypeName = $"{documentTypeName}InputCheckpoint";

            descriptor
                .Name("Query")
                .Field(pullDocumentsName)
                .Type<NonNullType<ObjectType<DocumentPullBulk<TDocument>>>>()
                .UseProjection()
                .UseFiltering()
                .Argument("checkpoint", a => a.Type(checkpointInputTypeName).Description($"The last known checkpoint for {documentTypeName} replication."))
                .Argument("limit", a => a.Type<NonNullType<IntType>>().Description($"The maximum number of {documentTypeName} documents to return."))
                .Description($"Pulls {documentTypeName} documents from the server based on the given checkpoint, limit, optional filters, and projections")
                .Resolve(context =>
                {
                    var queryResolver = context.Resolver<QueryResolver<TDocument>>();
                    var checkpoint = context.ArgumentValue<Checkpoint?>("checkpoint");
                    var limit = context.ArgumentValue<int>("limit");
                    var repository = context.Service<IDocumentRepository<TDocument>>();
                    var cancellationToken = context.RequestAborted;

                    return queryResolver.PullDocumentsAsync(checkpoint, limit, repository, context, cancellationToken);
                });
        }
    }

    private static IRequestExecutorBuilder ConfigureDocumentMutations<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder.AddTypeExtension<MutationExtension<TDocument>>();
    }

#pragma warning disable CA1812
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class MutationExtension<TDocument> : ObjectTypeExtension
#pragma warning restore CA1812
        where TDocument : class, IReplicatedDocument
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            var documentTypeName = typeof(TDocument).Name;
            var pushDocumentsName = $"push{documentTypeName}";
            var pushRowArgName = $"{char.ToLowerInvariant(documentTypeName[0])}{documentTypeName[1..]}PushRow";

            descriptor
                .Name("Mutation")
                .Field(pushDocumentsName)
                .Type<NonNullType<ListType<NonNullType<ObjectType<TDocument>>>>>()
                .Argument(pushRowArgName, a => a
                    .Type<ListType<InputObjectType<DocumentPushRow<TDocument>>>>()
                    .Description($"The list of {documentTypeName} documents to push to the server."))
                .Description($"Pushes {documentTypeName} documents to the server and handles any conflicts.")
                .Resolve(context =>
                {
                    var mutation = context.Resolver<MutationResolver<TDocument>>();
                    var documents = context.ArgumentValue<List<DocumentPushRow<TDocument>?>?>(pushRowArgName);
                    var cancellationToken = context.RequestAborted;

                    return mutation.PushDocumentsAsync(documents, cancellationToken);
                });
        }
    }

    private static IRequestExecutorBuilder ConfigureDocumentSubscriptions<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder
            .AddTypeExtension<SubscriptionExtension<TDocument>>()
            .AddType(new InputObjectType<Headers>(d =>
            {
                d.Name($"{typeof(TDocument).Name}InputHeaders");
                d.Field(f => f.Authorization)
                    .Type<NonNullType<StringType>>()
                    .Name("Authorization")
                    .Description("The JWT bearer token for authentication.");
            }));
    }

#pragma warning disable CA1812
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class SubscriptionExtension<TDocument> : ObjectTypeExtension
#pragma warning restore CA1812
        where TDocument : class, IReplicatedDocument
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            var documentTypeName = typeof(TDocument).Name;
            var streamDocumentName = $"stream{documentTypeName}";
            var headersInputTypeName = $"{documentTypeName}InputHeaders";

            descriptor
                .Name("Subscription")
                .Field(streamDocumentName)
                .Type<NonNullType<ObjectType<DocumentPullBulk<TDocument>>>>()
                .Argument("headers", a => a
                    .Type(headersInputTypeName)
                    .Description($"Headers for {documentTypeName} subscription authentication."))
                .Resolve(context => context.GetEventMessage<DocumentPullBulk<TDocument>>())
                .Subscribe(context =>
                {
                    var headers = context.ArgumentValue<Headers>("headers");
                    // You can add authentication logic here using the headers
                    if (!IsAuthorized(headers))
                    {
                        // Handle unauthorized access
                        throw new UnauthorizedAccessException("Invalid or missing authorization token");
                    }

                    var subscription = context.Resolver<SubscriptionResolver<TDocument>>();
                    return subscription.DocumentChangedStream(context.RequestAborted);
                });
        }

        private static bool IsAuthorized(Headers? headers)
        {
            if (headers != null)
            {
                // Implement your authorization logic here
                // For example, validate the JWT token in headers.Authorization
            }

            return true;
        }
    }

    private static void EnsureRootTypesExist(IRequestExecutorBuilder builder)
    {
        if (builder.Services.All(d => d.ServiceType != typeof(ObjectType<Query>)))
        {
            builder.AddQueryType<Query>();
        }
        if (builder.Services.All(d => d.ServiceType != typeof(ObjectType<Mutation>)))
        {
            builder.AddMutationType<Mutation>();
        }
        if (builder.Services.All(d => d.ServiceType != typeof(ObjectType<Subscription>)))
        {
            builder.AddSubscriptionType<Subscription>();
        }
    }
}
