using System.ComponentModel.DataAnnotations;
using HotChocolate;
using HotChocolate.Types;
using LiveDocs.GraphQLApi.Validations;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
/// Represents a user of the LiveDocs system.
/// </summary>
/// <remarks>
/// This class is part of a non-production example application and is not intended for use in a production environment.
/// The <see cref="JwtAccessToken"/> property is used to store a JWT access token for the user.
/// This token captures the user's identity, claims, and authorization information, allowing the system to simulate
/// user authentication without requiring a full authentication infrastructure.
/// In a production environment, it is highly recommended to implement a proper authentication and authorization system
/// instead of relying on this approach.
/// </remarks>
[GraphQLName("User")]
public sealed record ReplicatedUser : ReplicatedDocument
{
    /// <summary>
    /// The first name of the user.
    /// </summary>
    [Required]
    [Trim]
    [Length(1, 256)]
    public required string FirstName { get; init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [Required]
    [Trim]
    [Length(1, 256)]
    public required string LastName { get; init; }

    /// <summary>
    /// The full name of the user.
    /// </summary>
    public string? FullName
    {
        get => $"{FirstName} {LastName}".Trim();
        init{}
    }

    /// <summary>
    /// The email of the user. jThis must be globally unique.
    /// For simplicity in this example app, it cannot be updated.
    /// </summary>
    [Required]
    [Trim]
    [EmailAddress]
    [GraphQLType(typeof(EmailAddressType))]
    [MaxLength(256)]
    public required string Email { get; init; }

    /// <summary>
    /// A JWT access token used to simulate user authentication in a non-production environment.
    /// For simplicity in this example app, it cannot be updated.
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
    [JwtFormat(AllowNull = true)]
    [Trim]
    [MaxLength(2000)]
    public string? JwtAccessToken { get; init; }

    /// <summary>
    /// The client-assigned identifier of the workspace to which the user belongs.
    /// </summary>
    [Required]
    [NotDefault]
    public required Guid WorkspaceId { get; init; }

    public bool Equals(ReplicatedUser? other)
    {
        if (other is null || GetType() != other.GetType())
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other) &&
               string.Equals(FirstName, other.FirstName, StringComparison.Ordinal) &&
               string.Equals(LastName, other.LastName, StringComparison.Ordinal) &&
               string.Equals(Email, other.Email, StringComparison.Ordinal) &&
               string.Equals(JwtAccessToken, other.JwtAccessToken, StringComparison.Ordinal) &&
               WorkspaceId.Equals(other.WorkspaceId);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            base.GetHashCode(),
            FirstName,
            LastName,
            Email,
            JwtAccessToken,
            WorkspaceId
        );
    }
}
