using RxDBDotNet.Documents;
using System.ComponentModel.DataAnnotations;
using LiveDocs.GraphQLApi.Validations;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
///     Base class for a document that is replicated via RxDBDotNet.
/// </summary>
public abstract class ReplicatedDocument : IReplicatedDocument
{
    /// <inheritdoc />
    [Required]
    [NotDefault]
    public required Guid Id { get; init; }

    /// <inheritdoc />
    [Required]
    public required bool IsDeleted { get; set; }

    /// <inheritdoc />
    [Required]
    [NotDefault]
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <inheritdoc />
    [MaxLength(10)]
    public List<string>? Topics { get; set; }
}
