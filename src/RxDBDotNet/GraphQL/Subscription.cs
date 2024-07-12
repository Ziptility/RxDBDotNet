using HotChocolate.Subscriptions;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

public class Subscription<TDocument> where TDocument : class, IReplicatedDocument
{
    public IAsyncEnumerable<PullDocumentsResult<TDocument>> OnDocumentChangedStream(
        [Service] SubscriptionResolvers<TDocument> resolvers,
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
        => resolvers.Subscribe(eventReceiver, cancellationToken);

    [Subscribe(With = nameof(OnDocumentChangedStream))]
    public PullDocumentsResult<TDocument> OnDocumentChanged(
        [EventMessage] PullDocumentsResult<TDocument> changedDocument)
        => changedDocument;
}
