using System.Security.Claims;

namespace RxDBDotNet.Security;

/// <summary>
/// Provides configuration options for setting up security policies in RxDBDotNet.
/// This class allows for fine-grained control over access to replicated documents.
/// </summary>
/// <remarks>
/// When using the 'RequireMinimum...' methods, the TRole enum must be defined with the roles
/// in ascending order of authority. The enum values should correspond to the level of access,
/// with higher values indicating more authority.
/// <para>
/// Example of a correctly defined role enum:
/// <code>
/// public enum UserRole
/// {
///     StandardUser = 0,
///     WorkspaceAdmin = 1,
///     SystemAdmin = 2
/// }
/// </code>
/// </para>
/// With this enum, RequireMinimumRoleToRead(UserRole.StandardUser) would allow
/// StandardUser, WorkspaceAdmin, and SystemAdmin to read, while
/// RequireMinimumRoleToWrite(UserRole.WorkspaceAdmin) would only allow
/// WorkspaceAdmin and SystemAdmin to write.
/// </remarks>
public sealed class SecurityOptions
{
    internal List<AccessPolicy> Policies { get; } = [];

    /// <summary>
    /// Requires a minimum role for read access to the replicated document.
    /// </summary>
    /// <typeparam name="TRole">The enum type representing user roles. Must be defined with roles in ascending order of authority.</typeparam>
    /// <param name="minimumRole">The minimum role required for read access.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireMinimumRoleToRead<TRole>(TRole minimumRole)
        where TRole : struct, Enum
    {
        RequireAccess(AccessType.Read, user => user.IsInRoleOrHigher(minimumRole));
        return this;
    }

    /// <summary>
    /// Requires a minimum role for write access to the replicated document.
    /// </summary>
    /// <typeparam name="TRole">The enum type representing user roles. Must be defined with roles in ascending order of authority.</typeparam>
    /// <param name="minimumRole">The minimum role required for write access.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireMinimumRoleToWrite<TRole>(TRole minimumRole)
        where TRole : struct, Enum
    {
        RequireAccess(AccessType.Write, user => user.IsInRoleOrHigher(minimumRole));
        return this;
    }

    /// <summary>
    /// Requires a minimum role for delete access to the replicated document.
    /// </summary>
    /// <typeparam name="TRole">The enum type representing user roles. Must be defined with roles in ascending order of authority.</typeparam>
    /// <param name="minimumRole">The minimum role required for delete access.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireMinimumRoleToDelete<TRole>(TRole minimumRole)
        where TRole : struct, Enum
    {
        RequireAccess(AccessType.Delete, user => user.IsInRoleOrHigher(minimumRole));
        return this;
    }

    /// <summary>
    /// Requires a minimum role for specified access types to the replicated document.
    /// </summary>
    /// <typeparam name="TRole">The enum type representing user roles. Must be defined with roles in ascending order of authority.</typeparam>
    /// <param name="minimumRole">The minimum role required for the specified access types.</param>
    /// <param name="accessType">The types of access to require the minimum role for. Defaults to all access types.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireMinimumRole<TRole>(TRole minimumRole, AccessType accessType = AccessType.All)
        where TRole : struct, Enum
    {
        if (accessType.HasFlag(AccessType.Read))
        {
            RequireMinimumRoleToRead(minimumRole);
        }
        if (accessType.HasFlag(AccessType.Write))
        {
            RequireMinimumRoleToWrite(minimumRole);
        }
        if (accessType.HasFlag(AccessType.Delete))
        {
            RequireMinimumRoleToDelete(minimumRole);
        }
        return this;
    }

    /// <summary>
    /// Requires a custom condition to be met for read access to the replicated document.
    /// </summary>
    /// <param name="requirement">A function that takes a ClaimsPrincipal and returns a boolean indicating if the requirement is met.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireReadAccess(Func<ClaimsPrincipal, bool> requirement)
    {
        return RequireAccess(AccessType.Read, requirement);
    }

    /// <summary>
    /// Requires a custom condition to be met for write access to the replicated document.
    /// </summary>
    /// <param name="requirement">A function that takes a ClaimsPrincipal and returns a boolean indicating if the requirement is met.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireWriteAccess(Func<ClaimsPrincipal, bool> requirement)
    {
        return RequireAccess(AccessType.Write, requirement);
    }

    /// <summary>
    /// Requires a custom condition to be met for delete access to the replicated document.
    /// </summary>
    /// <param name="requirement">A function that takes a ClaimsPrincipal and returns a boolean indicating if the requirement is met.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireDeleteAccess(Func<ClaimsPrincipal, bool> requirement)
    {
        return RequireAccess(AccessType.Delete, requirement);
    }

    /// <summary>
    /// Requires a specific claim for read access to the replicated document.
    /// </summary>
    /// <param name="claimType">The type of the required claim.</param>
    /// <param name="claimValue">The required value of the claim.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireClaimForRead(string claimType, string claimValue)
    {
        return RequireAccess(AccessType.Read, user => user.HasClaim(claimType, claimValue));
    }

    /// <summary>
    /// Requires a specific claim for write access to the replicated document.
    /// </summary>
    /// <param name="claimType">The type of the required claim.</param>
    /// <param name="claimValue">The required value of the claim.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireClaimForWrite(string claimType, string claimValue)
    {
        return RequireAccess(AccessType.Write, user => user.HasClaim(claimType, claimValue));
    }

    /// <summary>
    /// Requires a specific claim for delete access to the replicated document.
    /// </summary>
    /// <param name="claimType">The type of the required claim.</param>
    /// <param name="claimValue">The required value of the claim.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireClaimForDelete(string claimType, string claimValue)
    {
        return RequireAccess(AccessType.Delete, user => user.HasClaim(claimType, claimValue));
    }

    /// <summary>
    /// Requires the user to be authenticated for the specified access types.
    /// </summary>
    /// <param name="accessType">The types of access to require authentication for. Defaults to all access types.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions RequireAuthentication(AccessType accessType = AccessType.All)
    {
        if (accessType.HasFlag(AccessType.Read))
        {
            RequireAccess(AccessType.Read, user => user.Identity?.IsAuthenticated ?? false);
        }
        if (accessType.HasFlag(AccessType.Write))
        {
            RequireAccess(AccessType.Write, user => user.Identity?.IsAuthenticated ?? false);
        }
        if (accessType.HasFlag(AccessType.Delete))
        {
            RequireAccess(AccessType.Delete, user => user.Identity?.IsAuthenticated ?? false);
        }
        return this;
    }

    private SecurityOptions RequireAccess(AccessType accessType, Func<ClaimsPrincipal, bool> requirement)
    {
        Policies.Add(new AccessPolicy(accessType, requirement));
        return this;
    }
}
