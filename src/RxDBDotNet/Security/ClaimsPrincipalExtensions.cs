using System.Globalization;
using System.Security.Claims;

namespace RxDBDotNet.Security;

/// <summary>
/// Provides extension methods for ClaimsPrincipal to work with role-based security in RxDBDotNet.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Checks if the principal has a role that is equal to or higher than the specified minimum role.
    /// </summary>
    /// <typeparam name="TRole">The enum type representing user roles. Must be defined with roles in ascending order of authority.</typeparam>
    /// <param name="principal">The ClaimsPrincipal to check.</param>
    /// <param name="minimumRole">The minimum role to check against.</param>
    /// <returns>True if the principal has a role equal to or higher than the minimum role, false otherwise.</returns>
    /// <remarks>
    /// This method assumes that the TRole enum is defined with roles in ascending order of authority,
    /// where higher enum values correspond to higher levels of access.
    /// </remarks>
    public static bool IsInRoleOrHigher<TRole>(this ClaimsPrincipal principal, TRole minimumRole)
        where TRole : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(principal);

        var userRoleClaim = principal.FindFirst(ClaimTypes.Role);
        if (userRoleClaim == null)
        {
            return false;
        }

        if (Enum.TryParse<TRole>(userRoleClaim.Value, out var userRole))
        {
            return Convert.ToInt32(userRole, CultureInfo.InvariantCulture) >=
                   Convert.ToInt32(minimumRole, CultureInfo.InvariantCulture);
        }

        return false;
    }
}
