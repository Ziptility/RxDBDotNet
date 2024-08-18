using System.Security.Claims;

namespace RxDBDotNet.Security;

/// <summary>
/// Represents a security policy for a specific type of access to a document.
/// </summary>
public sealed class AccessPolicy
{
    /// <summary>
    /// Gets the type of access this policy applies to.
    /// </summary>
    public AccessType Type { get; }

    /// <summary>
    /// Gets the requirement function that determines if access should be granted.
    /// </summary>
    public Func<ClaimsPrincipal, bool> Requirement { get; }

    /// <summary>
    /// Initializes a new instance of the AccessPolicy class.
    /// </summary>
    /// <param name="type">The type of access this policy applies to.</param>
    /// <param name="requirement">A function that takes a ClaimsPrincipal and returns a boolean indicating if the requirement is met.</param>
    public AccessPolicy(AccessType type, Func<ClaimsPrincipal, bool> requirement)
    {
        Type = type;
        Requirement = requirement;
    }
}
