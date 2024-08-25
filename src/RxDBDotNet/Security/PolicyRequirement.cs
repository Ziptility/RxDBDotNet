using Microsoft.AspNetCore.Authorization;

namespace RxDBDotNet.Security;

/// <summary>
/// Represents a security policy for a specific type of operation.
/// </summary>
public sealed record PolicyRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The operation this policy applies to.
    /// </summary>
    public required Operation Operation { get; init; }

    /// <summary>
    /// The policy that determines if operation should be granted.
    /// </summary>
    public required string Policy { get; init; }
}
