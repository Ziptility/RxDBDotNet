using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Models;

public class Hero : ReplicatedDocument
{
    [MaxLength(100)]
    public string? Name { get; init; }

    [Required]
    [MaxLength(30)]
    public required string Color { get; init; }
}
