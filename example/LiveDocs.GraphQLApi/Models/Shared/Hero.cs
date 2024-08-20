using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotChocolate;
using LiveDocs.GraphQLApi.Validations;
using RxDBDotNet.Documents;

namespace LiveDocs.GraphQLApi.Models.Shared;

[GraphQLName("Hero")]
public class Hero : IReplicatedDocument
{
    /// <inheritdoc />
    [Required]
    [NotDefault]
    public required Guid Id { get; init; }

    /// <inheritdoc />
    [Required]
    public required bool IsDeleted { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public List<string>? Topics { get; init; }

    /// <inheritdoc />
    [Required]
    [NotDefault]
    public required DateTimeOffset UpdatedAt { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(30)]
    public required string Color { get; set; }
}
