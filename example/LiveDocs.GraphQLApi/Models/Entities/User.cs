// example/LiveDocs.GraphQLApi/Models/Entities/User.cs

using System.ComponentModel.DataAnnotations;
using LiveDocs.GraphQLApi.Security;
using LiveDocs.GraphQLApi.Validations;

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
    /// A JWT access token used to simulate user authentication in a non-production environment.
    /// </summary>
    /// <remarks>
    /// This property is included for demonstration purposes only. It allows the system to simulate
    /// user authentication by capturing the user's identity, claims, and authorization information.
    /// A null value represents an anonymous user.
    /// <para>
    /// Do not use this approach in a production environment; instead, implement a proper authentication
    /// and authorization system.
    /// </para>
    /// </remarks>
    [JwtFormat]
    [MaxLength(2000)]
    public required string JwtAccessToken { get; set; }

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
