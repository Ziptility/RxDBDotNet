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

        return new DocumentChangeAsyncEnumerable(eventReceiver, logger, cancellationToken);
    }

    private sealed class DocumentChangeAsyncEnumerable : IAsyncEnumerable<DocumentPullBulk<TDocument>>
    {
        private readonly ITopicEventReceiver _eventReceiver;
        private readonly ILogger<SubscriptionResolver<TDocument>> _logger;
        private readonly CancellationToken _cancellationToken;

        public DocumentChangeAsyncEnumerable(
            ITopicEventReceiver eventReceiver,
            ILogger<SubscriptionResolver<TDocument>> logger,
            CancellationToken cancellationToken)
        {
            _eventReceiver = eventReceiver;
            _logger = logger;
            _cancellationToken = cancellationToken;
        }

        public IAsyncEnumerator<DocumentPullBulk<TDocument>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);
            return new DocumentChangeAsyncEnumerator(_eventReceiver, _logger, cancellationTokenSource.Token);
        }
    }

    private sealed class DocumentChangeAsyncEnumerator : IAsyncEnumerator<DocumentPullBulk<TDocument>>
    {
        private readonly Channel<DocumentPullBulk<TDocument>> _channel;
        private Task? _processTask;
        private readonly CancellationTokenSource _cts;
        private readonly ITopicEventReceiver _eventReceiver;
        private readonly ILogger<SubscriptionResolver<TDocument>> _logger;

        public DocumentChangeAsyncEnumerator(
            ITopicEventReceiver eventReceiver,
            ILogger<SubscriptionResolver<TDocument>> logger,
            CancellationToken cancellationToken)
        {
            _channel = Channel.CreateUnbounded<DocumentPullBulk<TDocument>>(new UnboundedChannelOptions
            {
                SingleReader = true, // Optimize for single reader
                SingleWriter = true, // We know we have only one writer
            });
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _eventReceiver = eventReceiver;
            _logger = logger;
        }

        public DocumentPullBulk<TDocument> Current { get; private set; } = default!;

        public async ValueTask<bool> MoveNextAsync()
        {
            _processTask ??= ProcessEventsAsync(_cts.Token);

            try
            {
                if (await _channel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false)
                    && _channel.Reader.TryRead(out var item))
                {
                    Current = item;
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                // Subscription was cancelled, which is a normal termination condition
            }

            return false;
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            if (_processTask != null)
            {
                await _processTask.ConfigureAwait(false);
            }
            _cts.Dispose();
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            var streamName = $"Stream_{typeof(TDocument).Name}";

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var documentSourceStream = await _eventReceiver.SubscribeAsync<DocumentPullBulk<TDocument>>(streamName, cancellationToken).ConfigureAwait(false);

                    await foreach (var pullDocumentResult in documentSourceStream.ReadEventsAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        await _channel.Writer.WriteAsync(pullDocumentResult, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Document change stream for {DocumentType} was cancelled.", typeof(TDocument).Name);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing the document change stream for {DocumentType}. Retrying in 5 seconds.", typeof(TDocument).Name);
                    await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                }
            }

            _channel.Writer.Complete();
        }
    }
}
