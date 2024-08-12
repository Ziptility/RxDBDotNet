using System.ComponentModel.DataAnnotations;
using HotChocolate;
using LiveDocs.GraphQLApi.Models.Shared;
using LiveDocs.GraphQLApi.Validations;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
/// Represents a user of the LiveDocs system.
/// </summary>
[GraphQLName("User")]
public class ReplicatedUser : ReplicatedDocument
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
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; set; }

    /// <summary>
    /// The role of the user.
    /// </summary>
    [Required]
    public required UserRole Role { get; set; }

    /// <summary>
    /// The unique identifier of the workspace to which the user belongs.
    /// </summary>
    [Required]
    [NotDefault]
    public required Guid WorkspaceId { get; init; }
}
