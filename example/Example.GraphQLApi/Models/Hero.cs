using System.ComponentModel.DataAnnotations;
using RxDBDotNet.Documents;

namespace Example.GraphQLApi.Models;

public class Hero : IReplicatedDocument
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; init; }

    [Required]
    [MaxLength(30)]
    public required string Color { get; init; }

    [GraphQLType(typeof(IdType))]
    [Required]
    public required Guid Id { get; init; }

    [Required]
    public required DateTimeOffset UpdatedAt { get; init; }

    public bool IsDeleted { get; init; }
}
