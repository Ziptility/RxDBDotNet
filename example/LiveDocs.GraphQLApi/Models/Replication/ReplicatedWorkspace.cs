using System.ComponentModel.DataAnnotations;
using HotChocolate;
using LiveDocs.GraphQLApi.Validations;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
/// Represents a workspace in the LiveDocs system, designed for synchronization via RxDBDotNet.
/// </summary>
/// <remarks>
/// A workspace is a container for users and documents, providing isolation and organization
/// within the LiveDocs platform. Each workspace has a unique name across the entire system.
/// </remarks>
[GraphQLName("Workspace")]
public sealed record ReplicatedWorkspace : ReplicatedDocument
{
    /// <summary>
    /// The name of the workspace. This must be globally unique.
    /// </summary>
    [Required]
    [Trim]
    [MaxLength(30)]
    public required string Name { get; init; }
}
