using System.ComponentModel.DataAnnotations;
using RxDBDotNet.Documents;

namespace LiveDocs.GraphQLApi.Models;

/// <summary>
/// Represents a workspace in the LiveDocs system.
/// </summary>
/// <remarks>
/// A workspace is a container for users and documents, providing isolation and organization
/// within the LiveDocs platform. Each workspace has a unique name across the entire system.
/// </remarks>
public class Workspace : IReplicatedDocument
{
    /// <summary>
    /// Gets or initializes the unique identifier for the workspace.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the name of the workspace. This must be globally unique.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the workspace was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the workspace has been deleted.
    /// </summary>
    public required bool IsDeleted { get; set; }
}
