using System.Runtime.CompilerServices;
using HotChocolate.Subscriptions;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.GraphQL;

/// <summary>
/// Extends the GraphQL subscription type to support real-time updates for replicated documents.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated. Must implement <see cref="IReplicatedDocument"/>.</typeparam>
/// <remarks>
/// This class is part of the RxDBDotNet project and provides GraphQL subscription functionality
/// for real-time updates to replicated documents. It works in conjunction with the RxDB replication protocol
/// to ensure that clients can receive live updates as documents change on the server.
/// </remarks>
[ExtendObjectType("Subscription")]
public class SubscriptionExtension<TDocument> where TDocument : class, IReplicatedDocument
{
    /// <summary>
    /// Provides a stream of document changes for subscription.
    /// </summary>
    /// <param name="eventReceiver">The topic event receiver for handling subscription events.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of <see cref="DocumentPullBulk{TDocument}"/> representing the stream of document changes.</returns>
    public async IAsyncEnumerable<DocumentPullBulk<TDocument>> OnDocumentChangedStream(
        [Service] ITopicEventReceiver eventReceiver,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamName = $"Stream_{typeof(TDocument).Name}";

        var documentSourceStream = await eventReceiver.SubscribeAsync<DocumentPullBulk<TDocument>>(streamName, cancellationToken);

        await foreach (var pullDocumentResult in documentSourceStream.ReadEventsAsync()
                           .WithCancellation(cancellationToken))
        {
            yield return pullDocumentResult;
        }
    }

    /// <summary>
    /// Handles individual document change events within the subscription stream.
    /// </summary>
    /// <param name="changedDocument">The result containing the changed document(s) and updated checkpoint.</param>
    /// <returns>The <see cref="DocumentPullBulk{TDocument}"/> representing the document change.</returns>
    /// <remarks>
    /// This method is decorated with the [Subscribe] attribute and is linked to the OnDocumentChangedStream method.
    /// It processes each individual document change event in the subscription stream and makes it available to GraphQL clients.
    /// </remarks>
    [Subscribe(With = nameof(OnDocumentChangedStream))]
    public DocumentPullBulk<TDocument> OnDocumentChanged([EventMessage] DocumentPullBulk<TDocument> changedDocument)
    {
        return changedDocument;
    }
}
