using System.ComponentModel.DataAnnotations;
using HotChocolate;
using LiveDocs.GraphQLApi.Validations;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
/// Represents a document that can be collaboratively edited in real-time
/// by multiple users within the same workspace.
/// </summary>
[GraphQLName("LiveDoc")]
public sealed record ReplicatedLiveDoc : ReplicatedDocument
{
    /// <summary>
    /// The content of the live doc.
    /// </summary>
    [Required]
    [MaxLength(10_485_760)] // 10 MB in characters
    public required string Content { get; init; }

    /// <summary>
    /// The client-assigned identifier of the live doc's owner within the workspace.
    /// </summary>
    [Required]
    [NotDefault]
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// The client-assigned identifier of the workspace to which the live doc belongs.
    /// </summary>
    [Required]
    [NotDefault]
    public required Guid WorkspaceId { get; init; }
}
