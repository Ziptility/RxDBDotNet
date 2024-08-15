using Microsoft.AspNetCore.Authorization;

namespace RxDBDotNet.Security;

/// <summary>
/// Handles the evaluation of access requirements for RxDBDotNet documents.
/// </summary>
public sealed class AccessRequirementHandler : AuthorizationHandler<AccessRequirement>
{
    /// <summary>
    /// Handles the evaluation of an access requirement.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The access requirement to evaluate.</param>
    /// <returns>A task that represents the asynchronous authorization operation.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var user = context.User;
        var policies = requirement.SecurityOptions.Policies
            .Where(p => p.Type.HasFlag(requirement.Type));

        foreach (var policy in policies)
        {
            if (!policy.Requirement(user))
            {
                return Task.CompletedTask;
            }
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
