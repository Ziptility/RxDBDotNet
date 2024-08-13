using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
/// Represents a workspace in the LiveDocs system.
/// </summary>
/// <remarks>
/// A workspace is a container for users and collaborative documents, providing isolation and organization
/// within the LiveDocs platform. Each workspace has a unique name across the entire system.
/// </remarks>
public sealed class Workspace : ReplicatedEntity
{
    /// <summary>
    /// The name of the workspace. This must be globally unique.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public required string Name { get; set; }
}
