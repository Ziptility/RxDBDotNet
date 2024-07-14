using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.Replication
{
    /// <summary>
    /// Manages checkpoint-related operations for the replication process.
    /// This class works with any document type that implements IReplicatedDocument,
    /// providing flexibility for different document structures.
    /// </summary>
    public static class CheckpointManager
    {
        /// <summary>
        /// Filters and orders a list of documents based on a given checkpoint.
        /// </summary>
        /// <param name="documents">The list of documents to filter and order.</param>
        /// <param name="checkpoint">The checkpoint to use for filtering.</param>
        /// <typeparam name="TDocument">The type of document being filtered, which must implement IReplicatedDocument.</typeparam>
        /// <returns>A filtered and ordered list of documents.</returns>
        public static IQueryable<TDocument> FilterAndOrderDocuments<TDocument>(this IQueryable<TDocument> documents, Checkpoint? checkpoint)
            where TDocument : IReplicatedDocument
        {
            return documents
                .Where(d => checkpoint == null ||
                            d.UpdatedAt > checkpoint.UpdatedAt ||
                            (d.UpdatedAt == checkpoint.UpdatedAt && d.Id.CompareTo(checkpoint.LastDocumentId) > 0))
                .OrderBy(d => d.UpdatedAt)
                .ThenBy(d => d.Id);
        }

        /// <summary>
        /// Creates a new checkpoint based on the last document in a list.
        /// </summary>
        /// <param name="documents">The list of documents to create a checkpoint from.</param>
        /// <param name="previousCheckpoint">The previous checkpoint, used if the document list is empty.</param>
        /// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
        /// <returns>A new checkpoint.</returns>
        public static Checkpoint CreateCheckpoint<TDocument>(this List<TDocument> documents, Checkpoint? previousCheckpoint)
            where TDocument : IReplicatedDocument
        {
            var lastDocument = documents.LastOrDefault();

            return new Checkpoint
            {
                LastDocumentId = lastDocument?.Id ?? previousCheckpoint?.LastDocumentId,
                UpdatedAt = lastDocument?.UpdatedAt ?? previousCheckpoint?.UpdatedAt,
            };
        }
    }
}
