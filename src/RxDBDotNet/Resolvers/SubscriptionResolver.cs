// src\RxDBDotNet\Resolvers\SubscriptionResolver.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.Resolvers;

/// <summary>
/// Provides subscription functionality for real-time updates of documents.
/// This class implements the 'event observation' mode of the RxDB replication protocol
/// with a fail-fast approach for reliability and simplicity.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated.</typeparam>
public sealed class SubscriptionResolver<TDocument> where TDocument : IReplicatedDocument
{
    /// <summary>
    /// Provides a stream of document changes for a subscription.
    /// </summary>
    /// <param name="eventReceiver">The event receiver for document changes.</param>
    /// <param name="topics">Optional topics to filter events.</param>
    /// <param name="cancellationToken">Cancellation token to stop the stream.</param>
    /// <returns>An async enumerable of document changes.</returns>
    /// <exception cref="ArgumentNullException">When eventReceiver is null.</exception>
#pragma warning disable CA1822
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
        List<string>? topics,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamName = $"Stream_{typeof(TDocument).Name}";

        var sourceStream = await eventReceiver
            .SubscribeAsync<DocumentPullBulk<TDocument>>(streamName, cancellationToken)
            .ConfigureAwait(false);

        await using (sourceStream.ConfigureAwait(false))
        {
            await foreach (var result in sourceStream.ReadEventsAsync()
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (ShouldYieldDocument(result, topics))
                {
                    yield return result;
                }
            }
        }
    }

    /// <summary>
    /// Determines if a document should be yielded based on topic filtering.
    /// Empty document sets are always yielded to maintain checkpoint consistency.
    /// Documents are filtered based on topic subscription if topics are specified.
    /// </summary>
    /// <param name="result">The bulk result of the document pull operation.</param>
    /// <param name="topics">The list of topics to filter the documents by. If null or empty, all documents are yielded.</param>
    private static bool ShouldYieldDocument(
        DocumentPullBulk<TDocument> result,
        List<string>? topics)
    {
        if (result.Documents.Count == 0)
        {
            return true; // Always yield empty updates to maintain checkpoint consistency
        }

        return topics is null
            || topics.Count == 0
            || result.Documents.Exists(doc =>
                doc.Topics?.Intersect(topics, StringComparer.Ordinal).Any() == true);
    }
}
