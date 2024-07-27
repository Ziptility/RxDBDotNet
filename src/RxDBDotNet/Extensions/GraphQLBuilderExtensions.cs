using System.Reflection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Repositories;
using RxDBDotNet.Resolvers;
using RxDBDotNet.Services;

namespace RxDBDotNet.Extensions;

/// <summary>
///     Provides extension methods for configuring GraphQL services with RxDBDotNet replication support.
///     This class integrates the RxDB replication protocol with Hot Chocolate GraphQL server,
///     enabling seamless real-time data synchronization between clients and the server.
/// </summary>
public static class GraphQLBuilderExtensions
{
    /// <summary>
    ///     Adds replication support for RxDBDotNet to the GraphQL schema.
    ///     This method configures all necessary services and types for the RxDB replication protocol.
    /// </summary>
    /// <param name="builder">The IRequestExecutorBuilder to configure.</param>
    /// <returns>The configured IRequestExecutorBuilder for method chaining.</returns>
    /// <remarks>
    ///     This method should be called once before adding support for specific document types.
    ///     It registers core services like IEventPublisher that are shared across all document types.
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

        return builder.InitializeOnStartup();
    }

    /// <summary>
    ///     Adds replication support for a specific document type to the GraphQL schema.
    ///     This method configures all necessary types, queries, mutations, and subscriptions for the RxDB replication
    ///     protocol.
    /// </summary>
    /// <typeparam name="TDocument">The type of document to support, which must implement IReplicatedDocument.</typeparam>
    /// <param name="builder">The HotChocolate IRequestExecutorBuilder to configure.</param>
    /// <returns>The configured IRequestExecutorBuilder for method chaining.</returns>
    /// <remarks>
    ///     <para>
    ///         This method should be called for each document type that needs to be replicated.
    ///         It's typically used in the ConfigureServices method of the Startup class or in the
    ///         service configuration of a minimal API setup.
    ///     </para>
    ///     <para>
    ///         Usage example:
    ///         <code>
    /// services.AddGraphQLServer()
    ///     .AddReplicationServer()
    ///     .AddReplicatedDocument&lt;MyDocument&gt;()
    ///     .AddReplicatedDocument&lt;AnotherDocument&gt;();
    /// </code>
    ///     </para>
    /// </remarks>
    public static IRequestExecutorBuilder AddReplicatedDocument<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder.AddResolver<QueryResolver<TDocument>>()
            .AddResolver<MutationResolver<TDocument>>()
            .AddResolver<SubscriptionResolver<TDocument>>()
            .ConfigureDocumentTypes<TDocument>()
            .ConfigureDocumentQueries<TDocument>()
            .ConfigureDocumentMutations<TDocument>()
            .ConfigureDocumentSubscriptions<TDocument>();
    }

    private static string GetGraphQLTypeName<TDocument>() where TDocument : class, IReplicatedDocument
    {
        var attribute = typeof(TDocument).GetCustomAttribute<GraphQLNameAttribute>();
        return attribute?.Name ?? typeof(TDocument).Name;
    }

    private static IRequestExecutorBuilder ConfigureDocumentTypes<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var graphQLTypeName = GetGraphQLTypeName<TDocument>();
        return builder.AddCheckpointInputType<TDocument>(graphQLTypeName)
            .AddDocumentPullBulkType<TDocument>(graphQLTypeName)
            .AddDocumentPushRowInputType<TDocument>(graphQLTypeName);
    }

    private static IRequestExecutorBuilder AddCheckpointInputType<TDocument>(this IRequestExecutorBuilder builder, string graphQLTypeName)
        where TDocument : class, IReplicatedDocument
    {
        var checkpointInputTypeName = $"{graphQLTypeName}InputCheckpoint";

        return builder.AddType(new InputObjectType<Checkpoint>(inputObjectTypeDescriptor =>
        {
            inputObjectTypeDescriptor.Name(checkpointInputTypeName)
                .Description($"Input type for the checkpoint of {graphQLTypeName} replication.");
            inputObjectTypeDescriptor.Field(f => f.UpdatedAt)
                .Type<DateTimeType>()
                .Description("The timestamp of the last update included in the synchronization batch.");
            inputObjectTypeDescriptor.Field(f => f.LastDocumentId)
                .Type<IdType>()
                .Description("The ID of the last document included in the synchronization batch.");
        }));
    }

    private static IRequestExecutorBuilder AddDocumentPullBulkType<TDocument>(this IRequestExecutorBuilder builder, string graphQLTypeName)
        where TDocument : class, IReplicatedDocument
    {
        var pullBulkTypeName = $"{graphQLTypeName}PullBulk";

        return builder.AddType(new ObjectType<DocumentPullBulk<TDocument>>(objectTypeDescriptor =>
        {
            objectTypeDescriptor.Name(pullBulkTypeName)
                .Description($"Represents the result of a pull operation for {graphQLTypeName} documents.");
            objectTypeDescriptor.Field(f => f.Documents)
                .UseFiltering<TDocument>()
                .Description($"The list of {graphQLTypeName} documents pulled from the server.");
            objectTypeDescriptor.Field(f => f.Checkpoint)
                .Description("The new checkpoint after this pull operation.");
        }));
    }

    private static IRequestExecutorBuilder AddDocumentPushRowInputType<TDocument>(this IRequestExecutorBuilder builder, string graphQLTypeName)
        where TDocument : class, IReplicatedDocument
    {
        var pushRowTypeName = $"{graphQLTypeName}InputPushRow";

        return builder.AddType(new InputObjectType<DocumentPushRow<TDocument>>(inputObjectTypeDescriptor =>
        {
            inputObjectTypeDescriptor.Name(pushRowTypeName)
                .Description($"Input type for pushing {graphQLTypeName} documents to the server.");
            inputObjectTypeDescriptor.Field(f => f.AssumedMasterState)
                .Type<InputObjectType<TDocument>>()
                .Description("The assumed state of the document on the server before the push.");
            inputObjectTypeDescriptor.Field(f => f.NewDocumentState)
                .Type<NonNullType<InputObjectType<TDocument>>>()
                .Description("The new state of the document being pushed.");
        }));
    }

    private static IRequestExecutorBuilder ConfigureDocumentQueries<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder.AddTypeExtension<QueryExtension<TDocument>>();
    }

    private static IRequestExecutorBuilder ConfigureDocumentMutations<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder.AddTypeExtension<MutationExtension<TDocument>>();
    }

    private static IRequestExecutorBuilder ConfigureDocumentSubscriptions<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var graphQLTypeName = GetGraphQLTypeName<TDocument>();
        return builder.AddTypeExtension<SubscriptionExtension<TDocument>>()
            .AddType(new InputObjectType<Headers>(d =>
            {
                d.Name($"{graphQLTypeName}InputHeaders");
                d.Field(f => f.Authorization)
                    .Type<NonNullType<StringType>>()
                    .Name("Authorization")
                    .Description("The JWT bearer token for authentication.");
            }));
    }

    /// <summary>
    ///     Ensures that root types (Query, Mutation, Subscription) exist in the GraphQL schema.
    ///     If any root type has not been registered, this method will add the default RxDBDotNet root type.
    /// </summary>
    /// <param name="builder">The IRequestExecutorBuilder to configure.</param>
    /// <remarks>
    ///     This method allows for flexible schema configuration:
    ///     - If the user has already registered custom root types, those will be preserved.
    ///     - Default RxDBDotNet root types are only added for operations that don't have a registered type.
    ///     - This supports scenarios where users might want to use custom root types for some operations
    ///     while defaulting to RxDBDotNet types for others.
    ///     - Custom root types should follow the naming convention of starting with "Query", "Mutation", or "Subscription"
    ///     (case-insensitive).
    /// </remarks>
    private static void EnsureRootTypesExist(IRequestExecutorBuilder builder)
    {
        // Register a root Query type if not already added
        builder.ConfigureSchema(schemaBuilder => schemaBuilder.TryAddRootType(() => new ObjectType<Query>(), OperationType.Query));

        // Register a root Mutation type if not already added
        builder.ConfigureSchema(schemaBuilder => schemaBuilder.TryAddRootType(() => new ObjectType<Mutation>(), OperationType.Mutation));

        // Register a root Subscription type if not already added
        builder.ConfigureSchema(schemaBuilder => schemaBuilder.TryAddRootType(() => new ObjectType<Subscription>(), OperationType.Subscription));
    }

#pragma warning disable CA1812
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class QueryExtension<TDocument> : ObjectTypeExtension
#pragma warning restore CA1812
        where TDocument : class, IReplicatedDocument
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            var graphQLTypeName = GetGraphQLTypeName<TDocument>();
            var pullDocumentsName = $"pull{graphQLTypeName}";
            var checkpointInputTypeName = $"{graphQLTypeName}InputCheckpoint";

            descriptor.Name("Query")
                .Field(pullDocumentsName)
                .UseFiltering()
                .Type<NonNullType<ObjectType<DocumentPullBulk<TDocument>>>>()
                .Argument("checkpoint", a => a.Type(checkpointInputTypeName)
                    .Description($"The last known checkpoint for {graphQLTypeName} replication."))
                .Argument("limit", a => a.Type<NonNullType<IntType>>()
                    .Description($"The maximum number of {graphQLTypeName} documents to return."))
                .Description(
                    $"Pulls {graphQLTypeName} documents from the server based on the given checkpoint, limit, optional filters, and projections")
                .Resolve(context =>
                {
                    var queryResolver = context.Resolver<QueryResolver<TDocument>>();
                    var checkpoint = context.ArgumentValue<Checkpoint?>("checkpoint");
                    var limit = context.ArgumentValue<int>("limit");
                    var repository = context.Service<IDocumentRepository<TDocument>>();
                    var cancellationToken = context.RequestAborted;

                    return queryResolver.PullDocumentsAsync(checkpoint, limit, repository, context,
                        cancellationToken);
                });
        }
    }

#pragma warning disable CA1812
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class MutationExtension<TDocument> : ObjectTypeExtension
#pragma warning restore CA1812
        where TDocument : class, IReplicatedDocument
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            var graphQLTypeName = GetGraphQLTypeName<TDocument>();
            var pushDocumentsName = $"push{graphQLTypeName}";
            var pushRowArgName = $"{char.ToLowerInvariant(graphQLTypeName[0])}{graphQLTypeName[1..]}PushRow";

            descriptor.Name("Mutation")
                .Field(pushDocumentsName)
                .Type<NonNullType<ListType<NonNullType<ObjectType<TDocument>>>>>()
                .Argument(pushRowArgName, a => a.Type<ListType<InputObjectType<DocumentPushRow<TDocument>>>>()
                    .Description($"The list of {graphQLTypeName} documents to push to the server."))
                .Description($"Pushes {graphQLTypeName} documents to the server and handles any conflicts.")
                .Resolve(context =>
                {
                    var mutation = context.Resolver<MutationResolver<TDocument>>();
                    var documents = context.ArgumentValue<List<DocumentPushRow<TDocument>?>?>(pushRowArgName);
                    var cancellationToken = context.RequestAborted;

                    return mutation.PushDocumentsAsync(documents, cancellationToken);
                });
        }
    }

#pragma warning disable CA1812
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class SubscriptionExtension<TDocument> : ObjectTypeExtension
#pragma warning restore CA1812
        where TDocument : class, IReplicatedDocument
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            var graphQLTypeName = GetGraphQLTypeName<TDocument>();
            var streamDocumentName = $"stream{graphQLTypeName}";
            var headersInputTypeName = $"{graphQLTypeName}InputHeaders";

            descriptor.Name("Subscription")
                .Field(streamDocumentName)
                .Type<NonNullType<ObjectType<DocumentPullBulk<TDocument>>>>()
                .Argument("headers", a => a.Type(headersInputTypeName)
                    .Description($"Headers for {graphQLTypeName} subscription authentication."))
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
}
