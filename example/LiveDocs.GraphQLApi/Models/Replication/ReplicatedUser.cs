using System.ComponentModel.DataAnnotations;
using HotChocolate;
using HotChocolate.Types;
using LiveDocs.GraphQLApi.Models.Shared;
using LiveDocs.GraphQLApi.Validations;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
/// Represents a user of the LiveDocs system.
/// </summary>
[GraphQLName("User")]
public sealed record ReplicatedUser : ReplicatedDocument
{
    /// <summary>
    /// The first name of the user.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public required string FirstName { get; init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public required string LastName { get; init; }

    /// <summary>
    /// The full name of the user.
    /// </summary>
    public string FullName
    {
        get => $"{FirstName} {LastName}".Trim();
        init
        {
            
        }
    }

    /// <summary>
    /// The email of the user.
    /// </summary>
    [Required]
    [EmailAddress]
    [GraphQLType(typeof(EmailAddressType))]
    [MaxLength(256)]
    public required string Email { get; init; }

    /// <summary>
    /// The role of the user.
    /// </summary>
    [Required]
    public required UserRole Role { get; init; }

    /// <summary>
    /// The client-assigned identifier of the workspace to which the user belongs.
    /// </summary>
    [Required]
    [NotDefault]
    public required Guid WorkspaceId { get; init; }
}
