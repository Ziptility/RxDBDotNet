using System.ComponentModel.DataAnnotations;
using RxDBDotNet.Documents;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
///     Base class for an entity that is replicated via RxDBDotNet.
/// </summary>
public abstract class ReplicatedEntity : IReplicatedDocument
{
    /// <inheritdoc />
    [Required]
    public required Guid Id { get; init; }

    /// <inheritdoc />
    [Required]
    public required bool IsDeleted { get; set; }

    /// <inheritdoc />
    [Required]
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <inheritdoc />
    public List<string>? Topics { get; set; }
}
