using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
///     Base class for an entity that is replicated via RxDBDotNet.
/// </summary>
public abstract class ReplicatedEntity
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
#pragma warning disable CA2227 // Collection properties should be read only
    public List<string>? Topics { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}
