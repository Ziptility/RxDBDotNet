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
    public required string FirstName { get; set; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public required string LastName { get; set; }

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
    public required string Email { get; set; }

    /// <summary>
    /// The role of the user.
    /// </summary>
    [Required]
    public required UserRole Role { get; set; }

    /// <summary>
    /// The db-assigned primary key of the workspace to which the user belongs.
    /// </summary>
    [Required]
    [NotDefault]
    public required Guid WorkspaceId { get; init; }
}
