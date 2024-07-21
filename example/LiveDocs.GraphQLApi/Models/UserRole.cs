namespace LiveDocs.GraphQLApi.Models;

/// <summary>
/// Represents the possible roles a user can have in the LiveDocs system.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// A regular user with standard permissions.
    /// </summary>
    User,

    /// <summary>
    /// An administrator with elevated permissions within their workspace.
    /// </summary>
    Admin,

    /// <summary>
    /// A super administrator with system-wide permissions across all workspaces.
    /// </summary>
    SuperAdmin,
}
