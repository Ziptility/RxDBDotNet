using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Models.Replication;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
/// Represents an entity that can be collaboratively edited in real-time
/// by multiple users within the same workspace.
/// </summary>
public class LiveDoc : ReplicatedEntity<LiveDoc, ReplicatedLiveDoc>
{
    /// <summary>
    /// The content of the live doc.
    /// </summary>
    [MaxLength(10_485_760)] // 10 MB in characters
    public required string Content { get; set; }

    /// <summary>
    /// The unique identifier of the live doc's owner.
    /// </summary>
    [Required]
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// The unique identifier of the workspace to which the live doc belongs.
    /// </summary>
    [Required]
    public required Guid WorkspaceId { get; init; }

    public override Expression<Func<LiveDoc, ReplicatedLiveDoc>> MapToReplicatedDocument()
    {
        return liveDoc => new ReplicatedLiveDoc
        {
            Id = liveDoc.ReplicatedDocumentId,
            PrimaryKeyId = liveDoc.Id,
            Content = liveDoc.Content,
            OwnerId = liveDoc.OwnerId,
            WorkspaceId = liveDoc.WorkspaceId,
            IsDeleted = liveDoc.IsDeleted,
            UpdatedAt = liveDoc.UpdatedAt,
            Topics = liveDoc.Topics,
        };
    }
}
