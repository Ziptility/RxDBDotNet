using System.Security.Authentication;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;
using RxDBDotNet.Security;
using RxDBDotNet.Services;
using static RxDBDotNet.Extensions.DocumentExtensions;

namespace RxDBDotNet.Extensions;

/// <summary>
///     Provides extension methods for configuring GraphQL services with RxDBDotNet replication support.
///     This class integrates the RxDB replication protocol with Hot Chocolate GraphQL server,
///     enabling seamless real-time data synchronization between clients and the server.
/// </summary>
public static class GraphQLBuilderExtensions
{
    /// <summary>
    /// Adds replication support for RxDBDotNet to the GraphQL schema.
    /// This method configures all necessary services and types for the RxDB replication protocol.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="IRequestExecutorBuilder"/> for method chaining.</returns>
    /// <remarks>
    /// This method should be called once before adding support for specific document types.
    /// It registers core services like <see cref="IEventPublisher"/> that are shared across all document types.
    /// </remarks>
    public static IRequestExecutorBuilder AddReplication(this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IEventPublisher, DefaultEventPublisher>();

        builder.AddFiltering();

        builder.AddSocketSessionInterceptor<WebSocketJwtAuthInterceptor>();

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
    /// <param name="configure">
    ///     An optional configuration action to customize the replication options for the document type.
    /// </param>
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
    ///     // Mutation conventions must be enabled for replication to work
    ///     .AddMutationConventions()
    ///     .AddReplication()
    ///     .AddReplicatedDocument&lt;MyDocument&gt;()
    ///     .AddReplicatedDocument&lt;AnotherDocument&gt;();
    /// </code>
    ///     </para>
    /// </remarks>
    public static IRequestExecutorBuilder AddReplicatedDocument<TDocument>(
        this IRequestExecutorBuilder builder,
        Action<ReplicationOptions<TDocument>>? configure = null) where TDocument : class, IReplicatedDocument
    {
        ArgumentNullException.ThrowIfNull(builder);

        var replicationOptions = new ReplicationOptions<TDocument>();
        configure?.Invoke(replicationOptions);

        if (replicationOptions.Security.PolicyRequirements.Count > 0)
        {
            builder.Services.AddScoped<AuthorizationHelper>();
        }

        return builder.AddResolver<QueryResolver<TDocument>>()
            .AddResolver<MutationResolver<TDocument>>()
            .AddResolver<SubscriptionResolver<TDocument>>()
            .ConfigureDocumentQueries(replicationOptions)
            .ConfigureDocumentMutations(replicationOptions)
            .ConfigureDocumentSubscriptions(replicationOptions);
    }

    private static IRequestExecutorBuilder ConfigureDocumentQueries<TDocument>(
        this IRequestExecutorBuilder builder,
        ReplicationOptions<TDocument> replicationOptions) where TDocument : class, IReplicatedDocument
    {
        var graphQLTypeName = GetGraphQLTypeName<TDocument>();
        var pullBulkTypeName = $"{graphQLTypeName}PullBulk";
        var pullDocumentsName = $"pull{graphQLTypeName}";
        var checkpointInputTypeName = $"{graphQLTypeName}InputCheckpoint";

        builder.AddTypeExtension(new ObjectTypeExtension(objectTypeDescriptor =>
        {
            var queryField = objectTypeDescriptor.Name("Query")
                .Field(pullDocumentsName)
                .UseFiltering<TDocument>()
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
                    var service = context.Service<IDocumentService<TDocument>>();
                    var cancellationToken = context.RequestAborted;

                    return queryResolver.PullDocumentsAsync(checkpoint, limit, service, context, cancellationToken);
                });

            AddReadAuthorizationIfNecessary(queryField, replicationOptions);
        }));

        builder.AddType(new ObjectType<DocumentPullBulk<TDocument>>(objectTypeDescriptor =>
        {
            objectTypeDescriptor.Name(pullBulkTypeName)
                .Description($"Represents the result of a pull operation for {graphQLTypeName} documents.");
            objectTypeDescriptor.Field(f => f.Documents)
                .Type<NonNullType<ListType<NonNullType<ObjectType<TDocument>>>>>()
                .Description($"The list of {graphQLTypeName} documents pulled from the server.");
            objectTypeDescriptor.Field(f => f.Checkpoint)
                .Type<NonNullType<ObjectType<Checkpoint>>>()
                .Description("The new checkpoint after this pull operation.");
        }));

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

    private static void AddReadAuthorizationIfNecessary<TDocument>(
        IObjectFieldDescriptor queryField,
        ReplicationOptions<TDocument> replicationOptions) where TDocument : IReplicatedDocument
    {
        foreach (var readPolicyRequirement in replicationOptions.Security.PolicyRequirements.Where(pr =>
                     pr.DocumentOperation.Operation == Operation.Read))
        {
            queryField.Authorize(readPolicyRequirement.Policy);
        }
    }

    private static IRequestExecutorBuilder ConfigureDocumentMutations<TDocument>(
        this IRequestExecutorBuilder builder,
        ReplicationOptions<TDocument> replicationOptions) where TDocument : class, IReplicatedDocument
    {
        var graphQLTypeName = GetGraphQLTypeName<TDocument>();
        var pushRowTypeName = $"{graphQLTypeName}InputPushRow";
        var pushDocumentsName = $"push{graphQLTypeName}";
        var pushRowArgName = $"{char.ToLowerInvariant(graphQLTypeName[0])}{graphQLTypeName[1..]}PushRow";

        builder.AddTypeExtension(new ObjectTypeExtension(objectTypeDescriptor =>
         {
             var field = objectTypeDescriptor.Name("Mutation")
                 .Field(pushDocumentsName)
                 .UseMutationConvention()
                 .Type<NonNullType<ListType<NonNullType<ObjectType<TDocument>>>>>()
                 .Argument(pushRowArgName, a => a.Type<ListType<InputObjectType<DocumentPushRow<TDocument>>>>()
                     .Description($"The list of {graphQLTypeName} documents to push to the server."))
                 .Description($"Pushes {graphQLTypeName} documents to the server and detects any conflicts.")
                 .Resolve(context =>
                 {
                     var mutation = context.Resolver<MutationResolver<TDocument>>();
                     var documentService = context.Service<IDocumentService<TDocument>>();
                     var documents = context.ArgumentValue<List<DocumentPushRow<TDocument>?>?>(pushRowArgName);
                     var cancellationToken = context.RequestAborted;
                     var authorizationHelper = context.Services.GetService<AuthorizationHelper>();
                     var currentUser = context.GetUser();
                     var securityOptions = replicationOptions.Security;

                     return mutation.PushDocumentsAsync(documents, documentService, currentUser, securityOptions,
                         authorizationHelper, cancellationToken);
                 });

             AddFieldErrorTypes(field, replicationOptions);
         }));

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

    private static void AddFieldErrorTypes<TDocument>(IObjectFieldDescriptor field, ReplicationOptions<TDocument> replicationOptions)
        where TDocument : IReplicatedDocument
    {
        var addedErrorTypes = new HashSet<Type>();

        field.Error<AuthenticationException>();
        addedErrorTypes.Add(typeof(AuthenticationException));

        field.Error<UnauthorizedAccessException>();
        addedErrorTypes.Add(typeof(UnauthorizedAccessException));

        // update the foreach code to not add the AuthenticationException error type if it has already been added
        foreach (var errorType in replicationOptions.Errors)
        {
            if (!addedErrorTypes.Contains(errorType))
            {
                field.Error(errorType);
                addedErrorTypes.Add(errorType);
            }
        }
    }

    private static IRequestExecutorBuilder ConfigureDocumentSubscriptions<TDocument>(
        this IRequestExecutorBuilder builder,
        ReplicationOptions<TDocument> replicationOptions) where TDocument : IReplicatedDocument
    {
        var graphQLTypeName = GetGraphQLTypeName<TDocument>();
        var streamDocumentName = $"stream{graphQLTypeName}";
        var headersInputTypeName = $"{graphQLTypeName}InputHeaders";

        return builder.AddTypeExtension(new ObjectTypeExtension(objectTypeDescriptor =>
        {
            var subscriptionField = objectTypeDescriptor.Name("Subscription")
                .Field(streamDocumentName)
                .Type<NonNullType<ObjectType<DocumentPullBulk<TDocument>>>>()
                .Argument("headers", a => a
                    .Type(new InputObjectType(d =>
                    {
                        d.Name(headersInputTypeName);
                        d.Field("Authorization")
                            .Type<NonNullType<StringType>>()
                            .Description("The JWT bearer token for authentication.");
                    }))
                    .Description($"Headers for {graphQLTypeName} subscription authentication. " +
                                 "Note: This parameter is defined to maintain compatibility with the RxDB protocol, " +
                                 "but is not used in the RxDBDotNet implementation. Instead, clients should pass " +
                                 "the Authorization header in the HTTP request following standard practices for " +
                                 "Hot Chocolate subscriptions and ASP.NET applications."))
                .Argument("topics", a => a.Type<ListType<NonNullType<StringType>>>())
                .Description($"An optional set topics to receive events for when {graphQLTypeName} is upserted."
                             + $" If null then events will be received for all {graphQLTypeName} upserts.")
                .Resolve(context => context.GetEventMessage<DocumentPullBulk<TDocument>>())
                .Subscribe(context =>
                {
                    var subscription = context.Resolver<SubscriptionResolver<TDocument>>();
                    var topicEventReceiver = context.Service<ITopicEventReceiver>();
                    var topics = context.ArgumentValue<List<string>?>("topics");

                    return subscription.DocumentChangedStream(topicEventReceiver, topics, context.RequestAborted);
                });

            AddReadAuthorizationIfNecessary(subscriptionField, replicationOptions);
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
}
