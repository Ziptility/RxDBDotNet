using System.Text.Json.Serialization;
using HotChocolate;

namespace LiveDocs.GraphQLApi.Models.Shared;

/// <summary>
/// Represents the possible roles a user can have in the LiveDocs system.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>
    /// A regular user with standard permissions.
    /// </summary>
    [GraphQLName("User")]
    User,

    /// <summary>
    /// An administrator with elevated permissions within their workspace.
    /// </summary>
    [GraphQLName("Admin")]
    Admin,

    /// <summary>
    /// A super administrator with system-wide permissions across all workspaces.
    /// </summary>
    [GraphQLName("SuperAdmin")]
    SuperAdmin,
}
