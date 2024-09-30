using RxDBDotNet.Documents;

namespace RxDBDotNet.Services;

/// <summary>
/// Defines the contract for a service that handles document operations for replication in RxDBDotNet.
/// This interface supports asynchronous operations required by the RxDB replication protocol.
/// Implementors should ensure thread-safety and proper handling of concurrent operations.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed. Must implement IDocument.</typeparam>
public interface IDocumentService<TDocument> where TDocument : class, IReplicatedDocument
{
    /// <summary>
    /// Provides a queryable representation of all documents in the service.
    /// </summary>
    /// <remarks>
    /// This method is crucial for the RxDB replication protocol as it serves as the foundation for all document queries.
    /// Implementors should:
    /// <list type="bullet">
    /// <item><description>Return an IQueryable that represents all documents in the service.</description></item>
    /// <item><description>Ensure the returned IQueryable does not track entity changes for better performance in read scenarios.</description></item>
    /// <item><description>Not apply any limiting or sorting in this method. RxDBDotNet will handle that based on replication needs.</description></item>
    /// </list>
    /// </remarks>
    /// <returns>An IQueryable of documents that can be further refined by RxDBDotNet for replication purposes.</returns>
    IQueryable<TDocument> GetQueryableDocuments();

    /// <summary>
    /// Asynchronously executes a query and retrieves a list of documents based on the provided queryable.
    /// </summary>
    /// <remarks>
    /// This method is called by RxDBDotNet to materialize the query built on top of GetQueryableDocuments.
    /// Implementors should:
    /// <list type="bullet">
    /// <item><description>Efficiently execute the given query against the underlying data store.</description></item>
    /// <item><description>Handle any database-specific optimizations or error scenarios.</description></item>
    /// <item><description>Respect the cancellation token to allow for operation cancellation.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="query">The refined query to execute, derived from GetQueryableDocuments.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation, resulting in a list of retrieved documents.</returns>
    Task<List<TDocument>> ExecuteQueryAsync(IQueryable<TDocument> query, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single document by its unique identifier.
    /// </summary>
    /// <remarks>
    /// This method is used during conflict resolution in the replication process.
    /// Implementors should:
    /// <list type="bullet">
    /// <item><description>Efficiently fetch a single document based on its ID.</description></item>
    /// <item><description>Return null if no document with the given ID exists.</description></item>
    /// <item><description>Handle any potential database-specific exceptions.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">The unique identifier of the document to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>The retrieved document, or null if not found.</returns>
    Task<TDocument?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new document in the service.
    /// </summary>
    /// <remarks>
    /// This method is called during the replication process to add new documents.
    /// Implementors should:
    /// <list type="bullet">
    /// <item><description>Insert the document into the underlying data store.</description></item>
    /// <item><description>Generate any necessary metadata (e.g., creation timestamps) if not provided.</description></item>
    /// <item><description>Handle potential conflicts if a document with the same ID already exists.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="document">The document to create. Its ID should already be set.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>The created document, potentially with updated metadata.</returns>
    Task<TDocument> CreateDocumentAsync(TDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing document in the service.
    /// </summary>
    /// <remarks>
    /// This method is used to apply changes to existing documents during replication.
    /// Implementors should:
    /// <list type="bullet">
    /// <item><description>Update the existing document in the underlying data store.</description></item>
    /// <item><description>Handle scenarios where the document might not exist (e.g., throw an exception or create it).</description></item>
    /// <item><description>Update any necessary metadata (e.g., modification timestamps).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="document">The document with updated information. Its ID identifies the document to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>The updated document, potentially with refreshed metadata.</returns>
    Task<TDocument> UpdateDocumentAsync(TDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a document as deleted without physically removing it from the service.
    /// </summary>
    /// <remarks>
    /// This method implements soft delete functionality required by the RxDB replication protocol.
    /// Implementors should:
    /// <list type="bullet">
    /// <item><description>Set the IsDeleted flag of the document to true.</description></item>
    /// <item><description>Update any relevant metadata (e.g., deletion timestamp).</description></item>
    /// <item><description>Not physically delete the document from the data store.</description></item>
    /// <item><description>Handle scenarios where the document might not exist.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="document">The document to mark as deleted.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<TDocument> MarkAsDeletedAsync(TDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// Compares two documents to determine if they are equal in terms of their content.
    /// </summary>
    /// <remarks>
    /// This method is crucial for conflict detection in the replication process.
    /// Implementors should:
    /// <list type="bullet">
    /// <item><description>Perform a deep comparison of the documents' content.</description></item>
    /// <item><description>Consider all relevant fields for equality, not just UpdatedAt and IsDeleted.</description></item>
    /// <item><description>Implement comparison logic that aligns with the specific document structure and business rules.</description></item>
    /// <item><description>Ensure the comparison is performant, especially for large documents or high-frequency operations.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="document1">The first document to compare.</param>
    /// <param name="document2">The second document to compare.</param>
    /// <returns>True if the documents are considered equal, false otherwise.</returns>
    bool AreDocumentsEqual(TDocument document1, TDocument document2);

    /// <summary>
    /// Saves all changes made in the service as an atomic operation.
    /// </summary>
    /// <remarks>
    /// This method is crucial for maintaining data consistency during the replication process.
    /// Implementors should:
    /// <list type="bullet">
    /// <item><description>Commit all pending changes to the underlying data store as a single transaction.</description></item>
    /// <item><description>Handle any potential concurrency conflicts.</description></item>
    /// <item><description>Implement appropriate error handling and rollback mechanisms.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
