using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;

namespace RxDBDotNet.Resolvers;

/// <summary>
///     Provides data replication resolvers for entities.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated.</typeparam>
/// <typeparam name="TContext">The type of the DbContext.</typeparam>
/// <remarks>
///     Initializes a new instance of the <see cref="ReplicationResolvers{TDocument, TContext}" /> class.
/// </remarks>
/// <param name="dbContext">The DbContext to be used for data access.</param>
public class ReplicationResolvers<TDocument, TContext>(TContext dbContext) where TDocument : class, IReplicatedDocument where TContext : DbContext
{
    /// <summary>
    ///     Pulls data from the backend based on the given checkpoint and limit.
    /// </summary>
    /// <param name="checkpoint">The last known checkpoint.</param>
    /// <param name="limit">The maximum number of documents to return.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a
    ///     <see cref="PullDocumentsResult{TDocument}" /> object containing the pulled documents and the new checkpoint.
    /// </returns>
    /// <remarks>
    ///     This method filters documents based on both <c>UpdatedAt</c> and <c>Id</c> to ensure it only includes documents
    ///     that are newer or have a higher <c>Id</c> than the checkpoint. The documents are sorted by <c>UpdatedAt</c>
    ///     and then by <c>Id</c> to maintain a consistent order. The checkpoint is set to the <c>Id</c> and <c>UpdatedAt</c>
    ///     of the last document in the batch. If no documents are found, it falls back to the current checkpoint values.
    /// </remarks>
    public async Task<PullDocumentsResult<TDocument>> PullDocuments(Checkpoint checkpoint, int limit)
    {
        var documents = await dbContext.Set<TDocument>()
            .Where(e => e.UpdatedAt > checkpoint.UpdatedAt || e.UpdatedAt == checkpoint.UpdatedAt && e.Id.CompareTo(checkpoint.LastDocumentId) > 0)
            .OrderBy(e => e.UpdatedAt)
            .ThenBy(e => e.Id)
            .Take(limit)
            .ToListAsync();

        var lastDocument = documents.LastOrDefault();

        var newCheckpoint = new Checkpoint
        {
            LastDocumentId = lastDocument?.Id ?? checkpoint.LastDocumentId,
            UpdatedAt = lastDocument?.UpdatedAt ?? checkpoint.UpdatedAt,
        };

        return new PullDocumentsResult<TDocument>
        {
            Documents = documents,
            Checkpoint = newCheckpoint,
        };
    }

    /// <summary>
    ///     Pushes data to the backend and handles any conflicts.
    /// </summary>
    /// <param name="documents">The list of documents to push, including their assumed master state.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a list of conflicting documents,
    ///     if any.
    /// </returns>
    /// <remarks>
    ///     This method handles the interpretation of <c>AssumedMasterState</c> as per RxDB's expectations:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 If <c>AssumedMasterState</c> is null, the server assumes there is no prior state to compare against and
    ///                 directly checks for existing documents.
    ///                 If a document exists and has a different <c>UpdatedAt</c> timestamp, it is considered a conflict.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If the <c>AssumedMasterState</c> is not null and the <c>UpdatedAt</c> timestamp does not match, the
    ///                 document is added to the list of conflicts.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public async Task<List<TDocument>> PushDocuments(List<PushDocumentRequest<TDocument>> documents)
    {
        var conflicts = new List<TDocument>();

        foreach (var document in documents)
        {
            var existing = await dbContext.Set<TDocument>()
                .FindAsync(document.NewDocumentState.Id);
            if (existing != null)
            {
                if (document.AssumedMasterState == null || existing.UpdatedAt != document.AssumedMasterState.UpdatedAt)
                {
                    conflicts.Add(existing);
                }
                else
                {
                    dbContext.Entry(existing)
                        .CurrentValues.SetValues(document.NewDocumentState);
                }
            }
            else
            {
                dbContext.Set<TDocument>()
                    .Add(document.NewDocumentState);
            }
        }

        await dbContext.SaveChangesAsync();

        return conflicts;
    }
}
