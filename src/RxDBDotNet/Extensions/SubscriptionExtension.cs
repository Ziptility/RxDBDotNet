using HotChocolate.Subscriptions;
using Microsoft.Extensions.Logging;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;
using static RxDBDotNet.Extensions.DocumentExtensions;

namespace RxDBDotNet.Extensions;

#pragma warning disable CA1812
internal sealed class SubscriptionExtension<TDocument> : ObjectTypeExtension
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
            .Argument("topics", a => a.Type<ListType<NonNullType<StringType>>>())
            .Description($"An optional set topics to recieve events for when {graphQLTypeName} is upserted."
                         + $" If null then events will be recieved for all {graphQLTypeName} upserts.")
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
                var topicEventReceiver = context.Service<ITopicEventReceiver>();
                var logger = context.Service<ILogger<SubscriptionResolver<TDocument>>>();
                var topics = context.ArgumentValue<List<string>?>("topics");

                return subscription.DocumentChangedStream(topicEventReceiver, topics, logger, context.RequestAborted);
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
