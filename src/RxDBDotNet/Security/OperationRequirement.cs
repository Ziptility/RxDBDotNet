using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace RxDBDotNet.Security;

/// <summary>
/// Represents a security policy for a specific type of access to a document.
/// </summary>
public sealed record OperationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the type of access this policy applies to.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Gets the requirement function that determines if access should be granted.
    /// </summary>
    public Func<ClaimsPrincipal, bool> Requirement { get; }

    /// <summary>
    /// Initializes a new instance of the AccessPolicy class.
    /// </summary>
    /// <param name="operation">The type of access this policy applies to.</param>
    /// <param name="requirement">A function that takes a ClaimsPrincipal and returns a boolean indicating if the requirement is met.</param>
    public OperationRequirement(Operation operation, Func<ClaimsPrincipal, bool> requirement)
    {
        Operation = operation;
        Requirement = requirement;
    }
}
