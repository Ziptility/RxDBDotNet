using Microsoft.AspNetCore.Authorization;

namespace RxDBDotNet.Security;

/// <summary>
/// Represents a policy requirement for authorizing access to replicated documents in RxDBDotNet.
/// </summary>
/// <remarks>
/// This record is used to define the specific policy and document operation that must be satisfied
/// for authorization to succeed.
/// </remarks>
public sealed record PolicyRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the document operation associated with this policy requirement.
    /// </summary>
    /// <value>
    /// The <see cref="DocumentOperation"/> that specifies the type of operation
    /// to be performed on a document within the security context.
    /// </value>
    public required DocumentOperation DocumentOperation { get; init; }

    /// <summary>
    /// The policy that determines if operation should be granted.
    /// </summary>
    public required string Policy { get; init; }
}
