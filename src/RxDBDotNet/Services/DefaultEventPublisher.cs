// src\RxDBDotNet\Services\DefaultEventPublisher.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Subscriptions;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.Services;

/// <summary>
/// Implements the IEventPublisher interface to publish document change events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the EventPublisher class.
/// </remarks>
/// <param name="eventSender">The ITopicEventSender used to send events.</param>
public sealed class DefaultEventPublisher(ITopicEventSender eventSender) : IEventPublisher
{
    /// <inheritdoc/>
    public async Task PublishDocumentChangedEventAsync<TDocument>(TDocument changedDocument, CancellationToken cancellationToken)
        where TDocument : IReplicatedDocument
    {
        ArgumentNullException.ThrowIfNull(changedDocument);

        var streamName = $"Stream_{typeof(TDocument).Name}";
        var pullBulk = new DocumentPullBulk<TDocument>
        {
            Documents = [changedDocument],
            Checkpoint = new Checkpoint
            {
                LastDocumentId = changedDocument.Id,
                UpdatedAt = changedDocument.UpdatedAt,
            },
        };

        await eventSender.SendAsync(streamName, pullBulk, cancellationToken).ConfigureAwait(false);
    }
}
