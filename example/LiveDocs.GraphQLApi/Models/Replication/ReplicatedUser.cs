using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
    private readonly string _firstName;
    private readonly string _lastName;
    private readonly string _email;

    /// <summary>
    /// The first name of the user.
    /// </summary>
    [Required]
    [Length(1, 256)]
    public required string FirstName
    {
        get => _firstName;
        [MemberNotNull(nameof(_firstName))]
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _firstName = value.Trim();
        }
    }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [Required]
    [Length(1, 256)]
    public required string LastName
    {
        get => _lastName;
        [MemberNotNull(nameof(_lastName))]
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _lastName = value.Trim();
        }
    }

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
    public required string Email
    {
        get => _email;
        [MemberNotNull(nameof(_email))]
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _email = value.Trim();
        }
    }

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

    public bool Equals(ReplicatedUser? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return base.Equals(other)
               && _firstName == other._firstName
               && _lastName == other._lastName
               && _email == other._email
               && Role == other.Role
               && WorkspaceId.Equals(other.WorkspaceId);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), _firstName, _lastName, _email, (int)Role, WorkspaceId);
    }
}
