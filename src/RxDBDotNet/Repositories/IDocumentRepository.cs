using RxDBDotNet.Documents;

namespace RxDBDotNet.Repositories;

/// <summary>
/// Defines the contract for a repository that handles document operations for replication.
/// </summary>
/// <typeparam name="TDocument">The type of document being managed, which must implement IReplicatedDocument.</typeparam>
public interface IDocumentRepository<TDocument> where TDocument : class, IReplicatedDocument
{
    /// <summary>
    /// Retrieves documents based on a minimum UpdatedAt time and a limit.
    /// </summary>
    /// <param name="minUpdatedAt">The minimum UpdatedAt time to filter documents.</param>
    /// <param name="limit">The maximum number of documents to retrieve.</param>
    /// <returns>A list of documents ordered by UpdatedAt and then by Id.</returns>
    IQueryable<TDocument> GetDocuments(DateTimeOffset? minUpdatedAt, int limit);

    /// <summary>
    /// Retrieves a document by its ID.
    /// </summary>
    /// <param name="id">The ID of the document to retrieve.</param>
    /// <returns>The document if found, or null if not found.</returns>
    Task<TDocument?> GetDocumentByIdAsync(Guid id);

    /// <summary>
    /// Creates a new document.
    /// </summary>
    /// <param name="document">The document to create.</param>
    /// <returns>The created document.</returns>
    Task<TDocument> CreateDocumentAsync(TDocument document);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="document">The document with updated values.</param>
    /// <returns>The updated document.</returns>
    Task<TDocument> UpdateDocumentAsync(TDocument document);

    /// <summary>
    /// Saves all changes made in the repository.
    /// </summary>
    Task SaveChangesAsync();
}
