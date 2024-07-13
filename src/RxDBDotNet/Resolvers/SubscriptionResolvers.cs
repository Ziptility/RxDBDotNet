using System.Runtime.CompilerServices;
using HotChocolate.Subscriptions;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.Resolvers;

/// <summary>
///     Provides subscription resolvers for entities.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated.</typeparam>
/// <remarks>
///     Initializes a new instance of the <see cref="SubscriptionResolvers{TDocument}" /> class.
/// </remarks>
public class SubscriptionResolvers<TDocument> where TDocument : class, IReplicatedDocument
{
    /// <summary>
    ///     Subscribes to updates for the document type.
    /// </summary>
    /// <param name="eventReceiver">The event receiver for the subscription.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the subscription.</param>
    public async IAsyncEnumerable<PullDocumentsResult<TDocument>> Subscribe(
        [Service] ITopicEventReceiver eventReceiver,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamName = $"Stream_{typeof(TDocument).Name}";

        var documentSourceStream = await eventReceiver.SubscribeAsync<PullDocumentsResult<TDocument>>(streamName, cancellationToken);

        await foreach (var pullDocumentResult in documentSourceStream.ReadEventsAsync()
                           .WithCancellation(cancellationToken))
        {
            yield return pullDocumentResult;
        }
    }
}
