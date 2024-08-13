using LiveDocs.GraphQLApi.Models.Shared;
using System.ComponentModel.DataAnnotations;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
/// Represents a user of the LiveDocs system.
/// </summary>
public sealed class User : ReplicatedEntity
{
    /// <summary>
    /// The first name of the user.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public required string FirstName { get; set; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public required string LastName { get; set; }

    /// <summary>
    /// The email of the user.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public required string Email { get; set; }

    /// <summary>
    /// The role of the user.
    /// </summary>
    [Required]
    public required UserRole Role { get; set; }

    /// <summary>
    /// The server-generated primary key of the workspace to which the user belongs.
    /// </summary>
    [Required]
    public required Guid WorkspaceId { get; init; }

    /// <summary>
    /// The workspace to which the user belongs.
    /// </summary>
    public Workspace? Workspace { get; init; }
}
