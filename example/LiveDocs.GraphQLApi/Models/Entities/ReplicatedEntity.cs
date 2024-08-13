using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RxDBDotNet.Documents;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
///     Base class for an entity that is replicated via RxDBDotNet.
/// </summary>
public abstract class ReplicatedEntity<TEntity, TDocument>
    where TDocument : class, IReplicatedDocument
    where TEntity : ReplicatedEntity<TEntity, TDocument>
{
    /// <summary>
    /// The primary key for this entity.
    /// </summary>
    [Required]
    public required Guid Id { get; init; }

    /// <summary>
    /// The client-assigned identifier for this entity.
    /// This property is used for client-side identification and replication purposes.
    /// </summary>
    [Required]
    public required Guid ReplicatedDocumentId { get; init; }

    /// <summary>
    /// A value indicating whether the entity has been marked as deleted.
    /// </summary>
    [Required]
    public required bool IsDeleted { get; set; }

    /// <summary>
    /// The timestamp of the last update to the entity.
    /// </summary>
    [Required]
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// An optional list of topics to publish events to when an instance is upserted.
    /// </summary>
    public List<string>? Topics { get; init; }

    public abstract Expression<Func<TEntity, TDocument>> MapToReplicatedDocument();
}
