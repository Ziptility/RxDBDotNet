using LiveDocs.GraphQLApi.Models.Shared;
using LiveDocs.GraphQLApi.Validations;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Models.Replication;

namespace LiveDocs.GraphQLApi.Models.Entities;

/// <summary>
/// Represents a user of the LiveDocs system.
/// </summary>
public class User : ReplicatedEntity<User, ReplicatedUser>
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
    [NotDefault]
    public required Guid WorkspaceId { get; init; }

    public override Expression<Func<User, ReplicatedUser>> MapToReplicatedDocument()
    {
        return user => new ReplicatedUser
        {
            Id = user.ReplicatedDocumentId,
            PrimaryKeyId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            WorkspaceId = user.WorkspaceId,
            Email = user.Email,
            IsDeleted = user.IsDeleted,
            UpdatedAt = user.UpdatedAt,
            Topics = user.Topics,
        };
    }
}
