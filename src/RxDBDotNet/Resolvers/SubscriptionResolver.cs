using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.Resolvers;

/// <summary>
///     Provides subscription functionality for real-time updates of documents.
///     This class implements the 'event observation' mode of the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated. Must implement <see cref="IDocument" />.</typeparam>
/// <remarks>
///     Note that this class must not use constructor injection per:
///     https://chillicream.com/docs/hotchocolate/v13/server/dependency-injection#constructor-injection
/// </remarks>
public sealed class SubscriptionResolver<TDocument> where TDocument : IDocument
{
    /// <summary>
    ///     Provides a stream of document changes for subscription.
    ///     This method is the entry point for GraphQL subscriptions and implements
    ///     the server-side push mechanism of the RxDB replication protocol.
    /// </summary>
    /// <param name="eventReceiver">The event receiver used for subscribing to document changes.</param>
    /// <param name="topics">An optional set of topics to receive events for when a document is changed.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     An asynchronous enumerable of <see cref="DocumentPullBulk{TDocument}" /> representing the stream of document
    ///     changes.
    /// </returns>
#pragma warning disable CA1822 // disable Mark members as static since this is a class instantiated by DI
    internal IAsyncEnumerable<DocumentPullBulk<TDocument>> DocumentChangedStream(
        ITopicEventReceiver eventReceiver,
        List<string>? topics,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventReceiver);

        return DocumentChangedStreamInternal(eventReceiver, topics, cancellationToken);
    }

    private static async IAsyncEnumerable<DocumentPullBulk<TDocument>> DocumentChangedStreamInternal(
        ITopicEventReceiver eventReceiver,
        List<string>? subscriberTopics,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamName = $"Stream_{typeof(TDocument).Name}";

        while (!cancellationToken.IsCancellationRequested)
        {
            ISourceStream<DocumentPullBulk<TDocument>>? documentStream = null;

            try
            {
                documentStream = await eventReceiver.SubscribeAsync<DocumentPullBulk<TDocument>>(streamName, cancellationToken)
                    .ConfigureAwait(false);

                await foreach (var pullDocumentResult in documentStream.ReadEventsAsync()
                                   .WithCancellation(cancellationToken)
                                   .ConfigureAwait(false))
                {
                    if (ShouldYieldDocuments(pullDocumentResult, subscriberTopics))
                    {
                        yield return pullDocumentResult;
                    }
                }
            }
            finally
            {
                // Ensure we dispose of the stream if it was created
                if (documentStream != null)
                {
                    await documentStream.DisposeAsync()
                        .ConfigureAwait(false);
                }
            }
        }
    }

    private static bool ShouldYieldDocuments(DocumentPullBulk<TDocument> pullDocumentResult, List<string>? subscriberTopics)
    {
        // In the RxDB replication protocol:
        // 1. Empty document lists in updates are valid and should be processed.
        // 2. The checkpoint should always be updated, even if no documents are present.
        // 3. The client (subscriber) should receive these updates to keep its checkpoint current.
        return pullDocumentResult.Documents.Count == 0
               || pullDocumentResult.Documents.Exists(doc => subscriberTopics == null
                                                             || subscriberTopics.Count == 0
                                                             // Not ignoring case to follow the pub/sub pattern of case-sensitive channels in redis
                                                             || doc.Topics?.Intersect(subscriberTopics, StringComparer.Ordinal).Any() == true);
    }
}
