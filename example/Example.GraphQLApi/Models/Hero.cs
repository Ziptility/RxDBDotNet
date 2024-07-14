using System.ComponentModel.DataAnnotations;
using RxDBDotNet.Documents;

namespace Example.GraphQLApi.Models;

public class Hero : IReplicatedDocument
{
    [GraphQLType(typeof(IdType))]
    [Required]
    public required Guid Id { get; init; }
    
    [MaxLength(100)]
    public string? Name { get; init; }

    [Required]
    [MaxLength(30)]
    public required string Color { get; init; }

    [Required]
    public required DateTimeOffset UpdatedAt { get; init; }

    [Required]
    public required bool IsDeleted { get; init; }
}
