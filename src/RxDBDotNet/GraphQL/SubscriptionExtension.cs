﻿using HotChocolate.Subscriptions;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

[ExtendObjectType("Subscription")]
public class SubscriptionExtension<TDocument> where TDocument : class, IReplicatedDocument
{
    public IAsyncEnumerable<PullDocumentsResult<TDocument>> OnDocumentChangedStream(
        [Service] SubscriptionResolvers<TDocument> resolvers,
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
        return resolvers.Subscribe(eventReceiver, cancellationToken);
    }

    [Subscribe(With = nameof(OnDocumentChangedStream))]
    public PullDocumentsResult<TDocument> OnDocumentChanged([EventMessage] PullDocumentsResult<TDocument> changedDocument)
    {
        return changedDocument;
    }
}