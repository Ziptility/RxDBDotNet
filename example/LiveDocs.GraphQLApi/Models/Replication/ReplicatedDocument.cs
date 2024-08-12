using RxDBDotNet.Documents;
using System.ComponentModel.DataAnnotations;
using LiveDocs.GraphQLApi.Validations;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
///     Base record for a document that is replicated via RxDBDotNet.
/// </summary>
public abstract record ReplicatedDocument : IReplicatedDocument
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
    public List<string>? Topics { get; init; }
}
