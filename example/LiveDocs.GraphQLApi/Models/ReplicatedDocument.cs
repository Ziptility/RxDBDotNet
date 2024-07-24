using RxDBDotNet.Documents;
using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Models;

/// <summary>
///     Base class for a document that is replicated via RxDBDotNet.
/// </summary>
public abstract class ReplicatedDocument : IReplicatedDocument
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
}
