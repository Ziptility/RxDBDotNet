// example/LiveDocs.GraphQLApi/Security/UserRole.cs

using System.Text.Json.Serialization;
using HotChocolate;

namespace LiveDocs.GraphQLApi.Security;

/// <summary>
/// Defines the roles a user can have within the LiveDocs system,
/// determining their level of access and permissions.
/// </summary>
/// <remarks>
/// The roles are hierarchical, with each level having more permissions.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>
    /// A standard user with access to basic features like viewing and editing their own documents.
    /// </summary>
    [GraphQLName("StandardUser")]
    StandardUser = 0,

    /// <summary>
    /// A workspace administrator with permissions to manage users and settings within their own workspace.
    /// </summary>
    [GraphQLName("WorkspaceAdmin")]
    WorkspaceAdmin = 1,

    /// <summary>
    /// A system administrator with full control over all workspaces and system settings.
    /// </summary>
    [GraphQLName("SystemAdmin")]
    SystemAdmin = 2,
}
