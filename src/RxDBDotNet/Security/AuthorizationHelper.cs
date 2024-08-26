using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using RxDBDotNet.Documents;

namespace RxDBDotNet.Security;

/// <summary>
///     Provides methods to handle authorization for document operations.
/// </summary>
public sealed class AuthorizationHelper
{
    private readonly IAuthorizationService? _authorizationService;
    private readonly ILogger<AuthorizationHelper> _logger;

    public AuthorizationHelper(IAuthorizationService? authorizationService, ILogger<AuthorizationHelper> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task AuthorizeAsync<TDocument>(
        ClaimsPrincipal? currentUser,
        DocumentOperation documentOperation,
        SecurityOptions<TDocument>? securityOptions) where TDocument : class, IReplicatedDocument
    {
        ArgumentNullException.ThrowIfNull(documentOperation);

        if (_authorizationService != null && securityOptions != null)
        {
            if (currentUser != null)
            {
                foreach (var policyRequirement in securityOptions.PolicyRequirements)
                {
                    // Check if the operation and document type match
                    if (policyRequirement.DocumentOperation.Operation == documentOperation.Operation
                        && policyRequirement.DocumentOperation.DocumentType == documentOperation.DocumentType)
                    {
                        _logger.LogInformation("Checking policy {Policy} for operation {Operation} on {DocumentType}", policyRequirement.Policy,
                            documentOperation.Operation, documentOperation.DocumentType);

                        // Evaluate the named policy
                        var authorizationResult = await _authorizationService.AuthorizeAsync(currentUser, null, policyRequirement.Policy)
                            .ConfigureAwait(false);

                        if (!authorizationResult.Succeeded)
                        {
                            _logger.LogWarning("Authorization failed for policy {Policy}", policyRequirement.Policy);
                            throw new AuthenticationException($"Authorization failed for policy {policyRequirement.Policy}");
                        }

                        _logger.LogInformation("Authorization succeeded for policy {Policy}", policyRequirement.Policy);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Operation or document type mismatch. Required: {RequiredOperation} on {RequiredType}, Actual: {ActualOperation} on {ActualType}",
                            policyRequirement.DocumentOperation.Operation, policyRequirement.DocumentOperation.DocumentType,
                            documentOperation.Operation, documentOperation.DocumentType);
                    }
                }
            }
            else
            {
                throw new AuthenticationException("Current user is null. Authorization failed.");
            }
        }
    }
}
