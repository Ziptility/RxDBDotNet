namespace LiveDocs.GraphQLApi.Models;

/// <summary>
/// Represents a user of the LiveDocs system.
/// </summary>
public class User : ReplicatedDocument
{
    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets the email of the user.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the role of the user.
    /// </summary>
    public required UserRole Role { get; set; }

    /// <summary>
    /// Gets or initializes the unique identifier of the workspace to which the user belongs.
    /// </summary>
    public required Guid WorkspaceId { get; init; }
}
