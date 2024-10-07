// src\RxDBDotNet\Security\AuthorizationHelper.cs

using System;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using RxDBDotNet.Documents;

namespace RxDBDotNet.Security;

/// <summary>
/// Provides helper methods for authorizing users for specific document operations based on security policies.
/// </summary>
/// <param name="authorizationService">The service used to handle authorization checks.</param>
public sealed class AuthorizationHelper(IAuthorizationService? authorizationService)
{
    /// <summary>
    /// Authorizes a user for a specific document operation based on provided security options.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document being operated on.</typeparam>
    /// <param name="currentUser">The current user attempting the operation.</param>
    /// <param name="documentOperation">The operation being performed on the document.</param>
    /// <param name="securityOptions">The security options containing policy requirements for the document operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="documentOperation"/> is null.</exception>
    /// <exception cref="AuthenticationException">Thrown when the current user is not authenticated.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user does not have the necessary permissions.</exception>
    /// <returns>A task representing the asynchronous authorization operation.</returns>
    public async Task AuthorizeAsync<TDocument>(
        ClaimsPrincipal? currentUser,
        DocumentOperation documentOperation,
        SecurityOptions<TDocument>? securityOptions) where TDocument : IReplicatedDocument
    {
        ArgumentNullException.ThrowIfNull(documentOperation);

        if (authorizationService == null)
        {
            return;
        }

        if (securityOptions == null || securityOptions.PolicyRequirements.Count == 0)
        {
            return;
        }

        var matchingPolicyRequirement = securityOptions.PolicyRequirements.Find(pr =>
            pr.DocumentOperation.Operation == documentOperation.Operation &&
            pr.DocumentOperation.DocumentType == documentOperation.DocumentType);

        if (matchingPolicyRequirement == null)
        {
            return;
        }

        if (currentUser == null)
        {
            throw new AuthenticationException("Authentication required to access this resource.");
        }

        if (currentUser.Identity?.IsAuthenticated != true)
        {
            throw new AuthenticationException("Authentication required to access this resource.");
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(
                currentUser,
                resource: null,
                matchingPolicyRequirement.Policy).ConfigureAwait(false);

        if (!authorizationResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Access denied due to insufficient permissions.");
        }
    }
}
