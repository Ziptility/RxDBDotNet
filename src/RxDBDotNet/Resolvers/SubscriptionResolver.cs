using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.Logging;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.Resolvers;

/// <summary>
/// Provides subscription functionality for real-time updates of replicated documents.
/// This class implements the 'event observation' mode of the RxDB replication protocol.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated. Must implement <see cref="IReplicatedDocument"/>.</typeparam>
/// <remarks>
/// Note that this class must not use constructor injection per:
/// https://chillicream.com/docs/hotchocolate/v13/server/dependency-injection#constructor-injection
/// </remarks>
public sealed class SubscriptionResolver<TDocument> where TDocument : class, IReplicatedDocument
{
    /// <summary>
    /// Provides a stream of document changes for subscription.
    /// This method is the entry point for GraphQL subscriptions and implements
    /// the server-side push mechanism of the RxDB replication protocol.
    /// </summary>
    /// <param name="eventReceiver">The event receiver used for subscribing to document changes.</param>
    /// <param name="logger">The logger used for logging information and errors.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of <see cref="DocumentPullBulk{TDocument}"/> representing the stream of document changes.</returns>
#pragma warning disable CA1822 // disable Mark members as static since this is a class instantiated by DI
    internal IAsyncEnumerable<DocumentPullBulk<TDocument>> DocumentChangedStream(
        ITopicEventReceiver eventReceiver,
        ILogger<SubscriptionResolver<TDocument>> logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventReceiver);
        ArgumentNullException.ThrowIfNull(logger);

        return DocumentIteratorAsync(cancellationToken);

        async IAsyncEnumerable<DocumentPullBulk<TDocument>> DocumentIteratorAsync([EnumeratorCancellation] CancellationToken localCancellationToken)
        {
            // Create a unique stream name for this document type
            // This allows multiple document types to have separate streams
            var streamName = $"Stream_{typeof(TDocument).Name}";

            // Use a channel to decouple the event receiving and yielding processes
            // This is crucial for managing backpressure and ensuring smooth operation
            // when clients consume events at different rates
            var channel = Channel.CreateUnbounded<DocumentPullBulk<TDocument>>();

            // Start processing events in a separate task
            // This allows us to handle events asynchronously without blocking the main thread
            _ = ProcessEventsAsync(streamName, channel.Writer, eventReceiver, logger, localCancellationToken);

            // Yield items from the channel as they become available
            // This creates a push-based subscription stream that clients can consume
            await foreach (var item in channel.Reader.ReadAllAsync(localCancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Processes incoming document change events and writes them to the channel.
    /// This method implements the core logic of the subscription mechanism.
    /// </summary>
    /// <param name="streamName">The name of the event stream to subscribe to.</param>
    /// <param name="writer">The channel writer to which processed events are written.</param>
    /// <param name="eventReceiver">The event receiver used for subscribing to document changes.</param>
    /// <param name="logger">The logger used for logging information and errors.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    private static async Task ProcessEventsAsync(
        string streamName,
        ChannelWriter<DocumentPullBulk<TDocument>> writer,
        ITopicEventReceiver eventReceiver,
        ILogger<SubscriptionResolver<TDocument>> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Subscribe to the document change events using Hot Chocolate's event receiver
            // This creates a connection to the underlying pub/sub system
            var documentSourceStream = await eventReceiver.SubscribeAsync<DocumentPullBulk<TDocument>>(streamName, cancellationToken).ConfigureAwait(false);

            // Process each incoming event
            await foreach (var pullDocumentResult in documentSourceStream
                               .ReadEventsAsync()
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                // Write the event to the channel
                // If the channel is full (e.g., slow consumer), this will wait until space is available
                // This implements backpressure, preventing memory issues with fast producers and slow consumers
                await writer.WriteAsync(pullDocumentResult, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Log when the stream is cancelled (e.g., when the subscription is terminated)
            // This is not an error condition, but it's useful for debugging and monitoring
            logger.LogInformation("Document change stream for {DocumentType} was cancelled.", typeof(TDocument).Name);
        }
        catch (Exception ex)
        {
            // Log any unexpected errors
            // This could indicate issues with the pub/sub system or serialization problems
            logger.LogError(ex, "An error occurred while processing the document change stream for {DocumentType}.", typeof(TDocument).Name);
        }
        finally
        {
            // Ensure the channel is marked as complete when we exit the processing loop
            // This signals to consumers that no more items will be produced
            writer.Complete();
        }
    }
}
