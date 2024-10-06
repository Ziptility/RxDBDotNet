// example/LiveDocs.GraphQLApi/Security/PolicyNames.cs
namespace LiveDocs.GraphQLApi.Security;

/// <summary>
/// Contains constants representing the names of various security policies used within the GraphQL API.
/// </summary>
public static class PolicyNames
{
    /// <summary>
    /// Represents the security policy name for standard user access to basic features
    /// like viewing and editing their own documents.
    /// </summary>
    public const string HasStandardUserAccess = "HasStandardUserAccess";

    /// <summary>
    /// Represents the security policy name for granting workspace administrator access
    /// to manage users and settings within their own workspace.
    /// </summary>
    public const string HasWorkspaceAdminAccess = "HasWorkspaceAdminAccess";

    /// <summary>
    /// Represents the security policy name for granting system administrator access
    /// to manage all aspects of the system, including user management, system settings,
    /// and other high-level administrative tasks.
    /// </summary>
    public const string HasSystemAdminAccess = "HasSystemAdminAccess";
}
