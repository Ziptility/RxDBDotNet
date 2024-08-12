using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
/// Represents an entity that can be collaboratively edited in real-time
/// by multiple users within the same workspace.
/// </summary>
public class LiveDoc : ReplicatedEntity
{
    /// <summary>
    /// The content of the live doc.
    /// </summary>
    [MaxLength(10_485_760)] // 10 MB in characters
    public required string Content { get; set; }

    /// <summary>
    /// The unique identifier of the live doc's owner.
    /// </summary>
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// The unique identifier of the workspace to which the live doc belongs.
    /// </summary>
    public required Guid WorkspaceId { get; init; }
}
