using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Models;

/// <summary>
/// Represents a workspace in the LiveDocs system.
/// </summary>
/// <remarks>
/// A workspace is a container for users and documents, providing isolation and organization
/// within the LiveDocs platform. Each workspace has a unique name across the entire system.
/// </remarks>
public class Workspace : ReplicatedDocument
{
    /// <summary>
    /// Gets or sets the name of the workspace. This must be globally unique.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
}
