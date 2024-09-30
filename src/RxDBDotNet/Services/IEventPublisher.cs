using RxDBDotNet.Documents;

namespace RxDBDotNet.Services;

/// <summary>
/// Defines a service for publishing events related to document changes.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a document change event.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document that changed.</typeparam>
    /// <param name="changedDocument">The document that was changed.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishDocumentChangedEventAsync<TDocument>(TDocument changedDocument, CancellationToken cancellationToken)
        where TDocument : IReplicatedDocument;
}
