using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Models.Entities;

public class Topic
{
    /// <summary>
    /// The name of the topic.
    /// </summary>
    [MaxLength(50)]
    public required string Name { get; init; }
}
