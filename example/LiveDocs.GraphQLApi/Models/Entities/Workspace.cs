using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Models.Replication;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
/// Represents a workspace in the LiveDocs system.
/// </summary>
/// <remarks>
/// A workspace is a container for users and collaborative documents, providing isolation and organization
/// within the LiveDocs platform. Each workspace has a unique name across the entire system.
/// </remarks>
public class Workspace : ReplicatedEntity<Workspace, ReplicatedWorkspace>
{
    /// <summary>
    /// The name of the workspace. This must be globally unique.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public required string Name { get; set; }

    public override Expression<Func<Workspace, ReplicatedWorkspace>> MapToReplicatedDocument()
    {
        return workspace => new ReplicatedWorkspace
        {
            Id = workspace.ReplicatedDocumentId,
            PrimaryKeyId = workspace.Id,
            Name = workspace.Name,
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = workspace.UpdatedAt,
            Topics = workspace.Topics,
        };
    }
}
