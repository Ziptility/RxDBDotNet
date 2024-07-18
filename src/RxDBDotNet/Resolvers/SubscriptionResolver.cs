using System.Runtime.CompilerServices;
using HotChocolate.Subscriptions;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.Resolvers;

/// <summary>
/// Provides subscription functionality for real-time updates of replicated documents.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated. Must implement <see cref="IReplicatedDocument"/>.</typeparam>
public sealed class SubscriptionResolver<TDocument>(ITopicEventReceiver eventReceiver) where TDocument : class, IReplicatedDocument
{
    /// <summary>
    /// Provides a stream of document changes for subscription.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of <see cref="DocumentPullBulk{TDocument}"/> representing the stream of document changes.</returns>
    public async IAsyncEnumerable<DocumentPullBulk<TDocument>> DocumentChangedStream(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamName = $"Stream_{typeof(TDocument).Name}";

        var documentSourceStream = await eventReceiver.SubscribeAsync<DocumentPullBulk<TDocument>>(streamName, cancellationToken);

        await foreach (var pullDocumentResult in documentSourceStream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return pullDocumentResult;
        }
    }
}
