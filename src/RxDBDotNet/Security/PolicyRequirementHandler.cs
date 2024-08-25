using Microsoft.AspNetCore.Authorization;

namespace RxDBDotNet.Security;

/// <summary>
/// Handles the evaluation of policy requirements for replicated documents.
/// </summary>
public sealed class PolicyRequirementHandler : AuthorizationHandler<PolicyRequirement>
{
    /// <summary>
    /// Handles the evaluation of a policy requirement asynchronously.
    /// </summary>
    /// <param name="context">The authorization context containing information about the user and resource.</param>
    /// <param name="requirement">The policy requirement to evaluate against the context.</param>
    /// <returns>A task that represents the asynchronous operation of handling the requirement.</returns>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PolicyRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.HasClaim(c => string.Equals(c.Type, requirement.Policy, StringComparison.Ordinal)
                                       && Enum.TryParse<Operation>(c.Value, out var operation)
                                       && (operation & requirement.Operation) == requirement.Operation))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
