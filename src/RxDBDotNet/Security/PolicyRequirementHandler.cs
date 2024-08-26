using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace RxDBDotNet.Security;

/// <summary>
/// Handles the evaluation of policy requirements for replicated documents.
/// This handler is responsible for enforcing security policies on document operations.
/// </summary>
public sealed class PolicyRequirementHandler : AuthorizationHandler<PolicyRequirement>
{
    private readonly ILogger<PolicyRequirementHandler> _logger;
    private readonly IAuthorizationService _authorizationService;

    public PolicyRequirementHandler(ILogger<PolicyRequirementHandler> logger,
        IAuthorizationService authorizationService)
    {
        _logger = logger;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Handles the evaluation of a policy requirement asynchronously.
    /// </summary>
    /// <param name="context">The authorization context containing information about the user and resource.</param>
    /// <param name="requirement">The policy requirement to evaluate against the context.</param>
    /// <returns>A task that represents the asynchronous operation of handling the requirement.</returns>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PolicyRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        _logger.LogInformation("Evaluating PolicyRequirement for user {UserId}", context.User.Identity?.Name);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated. PolicyRequirement not satisfied.");
            return;
        }

        var documentOperation = context.Resource as DocumentOperation;
        if (documentOperation == null)
        {
            _logger.LogWarning("Resource is not a DocumentOperation. PolicyRequirement not satisfied.");
            return;
        }

        _logger.LogInformation("Checking if operation {Operation} on {DocumentType} matches requirement",
            documentOperation.Operation, documentOperation.DocumentType);

        if (documentOperation.Operation == requirement.DocumentOperation.Operation &&
            documentOperation.DocumentType == requirement.DocumentOperation.DocumentType)
        {
            _logger.LogInformation("Operation and document type match. Evaluating policy {Policy}", requirement.Policy);

            // Evaluate the named policy
            var policyEvaluationResult = await _authorizationService.AuthorizeAsync(
                context.User,
                context.Resource,
                requirement.Policy).ConfigureAwait(false);

            if (policyEvaluationResult.Succeeded)
            {
                _logger.LogInformation("Policy {Policy} evaluation succeeded. PolicyRequirement satisfied.", requirement.Policy);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Policy {Policy} evaluation failed. PolicyRequirement not satisfied.", requirement.Policy);
            }
        }
        else
        {
            _logger.LogWarning("Operation or document type do not match. PolicyRequirement not satisfied.");
        }
    }
}
