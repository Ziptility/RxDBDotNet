// src\RxDBDotNet\Security\SubscriptionJwtAuthInterceptor.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RxDBDotNet.Configuration;

namespace RxDBDotNet.Security;

/// <summary>
/// Middleware for authenticating WebSocket connections in GraphQL subscriptions.
/// This middleware implements part of the graphql-transport-ws protocol, specifically handling the ConnectionInit message.
/// It validates JWT tokens sent in the connection payload and sets up the ClaimsPrincipal for authenticated connections.
/// If JWT authentication is not configured, it allows all connections.
/// </summary>
/// <remarks>
/// This middleware should be registered with the GraphQL server using AddSocketSessionInterceptor&lt;SubscriptionAuthMiddleware&gt;().
/// It uses the same JWT configuration as set up in AddJwtBearer() for consistency across HTTP and WebSocket connections, if available.
/// <para>
/// According to the graphql-transport-ws protocol:
/// - The server must receive the connection initialisation message within the allowed waiting time.
/// - If the server wishes to reject the connection during authentication, it should close the socket with the event 4403: Forbidden.
/// - If the server receives more than one ConnectionInit message, it should close the socket with the event 4429: Too many initialisation requests.
/// </para>
/// Note: This implementation assumes that Hot Chocolate handles the connection timeout and multiple ConnectionInit messages internally.
/// If this is not the case, additional logic would need to be added to this middleware to fully comply with the protocol.
/// </remarks>
public class SubscriptionJwtAuthInterceptor : DefaultSocketSessionInterceptor
{
    private const string BearerPrefix = "Bearer ";
    private readonly IAuthenticationSchemeProvider? _schemeProvider;
    private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptionsMonitor;
    private readonly ILogger<SubscriptionJwtAuthInterceptor> _logger;
    private readonly IReadOnlyList<string> _authenticationSchemes;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionJwtAuthInterceptor"/> class.
    /// </summary>
    /// <param name="schemeProvider">The authentication scheme provider.</param>
    /// <param name="jwtOptionsMonitor">The options monitor for JWT bearer token validation.</param>
    /// <param name="replicationOptions">The replication options.</param>
    /// <param name="logger">The logger for this middleware.</param>
    /// <remarks>
    /// We inject <see cref="IAuthenticationSchemeProvider"/> and <see cref="IOptionsMonitor{JwtBearerOptions}"/> for the following reasons:
    /// 1. <see cref="IAuthenticationSchemeProvider"/> allows us to check if JWT Bearer authentication is configured.
    /// 2. <see cref="IOptionsMonitor{JwtBearerOptions}"/> provides access to the JWT configuration for token validation.
    /// This approach allows the middleware to work correctly whether authentication is configured or not.
    /// </remarks>
    public SubscriptionJwtAuthInterceptor(
        IAuthenticationSchemeProvider? schemeProvider,
        IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor,
        IOptions<ReplicationOptions> replicationOptions,
        ILogger<SubscriptionJwtAuthInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(replicationOptions);

        _schemeProvider = schemeProvider;
        _jwtOptionsMonitor = jwtOptionsMonitor;
        _logger = logger;
        _authenticationSchemes = replicationOptions.Value.Security.SubscriptionAuthenticationSchemes;
    }

    /// <inheritdoc/>
    public override async ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(connectionInitMessage);

        _logger.LogInformation("WebSocket connection attempt from {RemoteIpAddress}", session.Connection.HttpContext.Connection.RemoteIpAddress);
        _logger.LogDebug("Connection payload: {@Payload}", connectionInitMessage);

        try
        {
            // Check if JWT Bearer authentication is configured
            // This allows the middleware to work in both authenticated and non-authenticated setups
            var isJwtBearerConfigured = await IsJwtBearerConfiguredAsync().ConfigureAwait(false);
            _logger.LogDebug("JWT Bearer authentication is {ConfigurationStatus}", GetConfigurationStatus(isJwtBearerConfigured));

            if (isJwtBearerConfigured == null)
            {
                return RejectConnection("Unable to determine if JWT Bearer authentication is configured.");
            }

            if (!isJwtBearerConfigured.Value)
            {
                // If JWT Bearer is not configured, we accept all connections
                // This is crucial for supporting non-authenticated scenarios
                _logger.LogInformation("Accepting connection without authentication as JWT Bearer is not configured");
                return ConnectionStatus.Accept();
            }

            // JWT Bearer is configured, so we proceed with token validation
            var token = ExtractToken(connectionInitMessage);
            if (string.IsNullOrEmpty(token))
            {
                return RejectConnection("No valid authorization token provided in the connection payload");
            }

            _logger.LogDebug("Token extracted from connection payload. Proceeding with validation");

            // Try each configured authentication scheme until one succeeds
            foreach (var scheme in _authenticationSchemes)
            {
                var claimsPrincipal = await ValidateTokenAsync(token, scheme, cancellationToken).ConfigureAwait(false);
                if (claimsPrincipal != null)
                {
                    session.Connection.HttpContext.User = claimsPrincipal;
                    _logger.LogInformation("Connection authenticated successfully for user {UserId} using scheme {Scheme}", claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value, scheme);
                    return ConnectionStatus.Accept();
                }
            }

            // If we get here, no scheme successfully validated the token
            return RejectConnection("Token validation failed for all configured authentication schemes");
        }
        catch (Exception ex)
        {
            // Log the error and reject the connection
            // This ensures that we don't accidentally allow unauthorized access in case of errors
            _logger.LogError(ex, "An error occurred during WebSocket connection authentication.");
            return RejectConnection("An error occurred during authentication.");
        }
    }

    private static string GetConfigurationStatus(bool? isJwtBearerConfigured)
    {
        if (isJwtBearerConfigured == null)
        {
            return "unknown";
        }

        return isJwtBearerConfigured.Value ? "configured" : "not configured";
    }

    /// <summary>
    /// Extracts the JWT token from the connection initialization message.
    /// </summary>
    /// <param name="connectionInitMessage">The connection initialization message.</param>
    /// <returns>The extracted JWT token, or null if no valid token is found.</returns>
    private string? ExtractToken(IOperationMessagePayload connectionInitMessage)
    {
        var connectPayload = connectionInitMessage.As<SocketConnectPayload>();
        var authorizationHeader = connectPayload?.Headers.Authorization;

        _logger.LogDebug("Authorization header {HeaderPresence}", string.IsNullOrEmpty(authorizationHeader) ? "not present" : "present");

        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorizationHeader[BearerPrefix.Length..].Trim();
    }

    /// <summary>
    /// Creates a ConnectionStatus that rejects the connection with a specific reason.
    /// </summary>
    /// <param name="reason">The reason for rejecting the connection.</param>
    /// <returns>A ConnectionStatus indicating a rejected connection.</returns>
    private ConnectionStatus RejectConnection(string reason)
    {
#pragma warning disable CA2254
        _logger.LogWarning(reason);
#pragma warning restore CA2254

        return ConnectionStatus.Reject("4403: Forbidden", new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            { "reason", reason },
        });
    }

    /// <summary>
    /// Validates the provided JWT token using the configured JWT bearer options.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="scheme">The authentication scheme to use for validation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ClaimsPrincipal"/> if the token is valid and a non-null principal was created; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method uses the same validation parameters as configured in <c>AddJwtBearer()</c>,
    /// ensuring consistency between HTTP and WebSocket authentication.
    /// The method is designed to handle exceptions during token validation, returning null for any validation failure.
    /// This approach allows the calling method to easily distinguish between valid and invalid tokens.
    /// </remarks>
    private async Task<ClaimsPrincipal?> ValidateTokenAsync(
        string token,
        string scheme,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting token validation using scheme: {Scheme}", scheme);

        // Retrieve the JWT Bearer options. These options are configured when setting up JWT authentication
        var jwtBearerOptions = _jwtOptionsMonitor.Get(scheme);
        _logger.LogDebug("JWT Bearer options retrieved for scheme: {Scheme}", scheme);

        // Get the token handler from the options
        var tokenHandler = jwtBearerOptions.TokenHandlers.Single();
        _logger.LogDebug("Token handler retrieved: {TokenHandlerType}", tokenHandler.GetType().Name);

        // Get the token validation parameters from the options
        var validationParameters = jwtBearerOptions.TokenValidationParameters;
        _logger.LogDebug("Token validation parameters retrieved for scheme: {Scheme}", scheme);

        try
        {
            await TryUpdateValidationParametersAsync(jwtBearerOptions, validationParameters, cancellationToken)
                .ConfigureAwait(false);

            // Validate the token using the configured parameters
            // This step performs the actual cryptographic verification of the token
            _logger.LogDebug("Validating token using scheme: {Scheme}", scheme);
            var tokenValidationResult = await tokenHandler
                .ValidateTokenAsync(token, validationParameters)
                .ConfigureAwait(false);

            if (tokenValidationResult.IsValid)
            {
                _logger.LogInformation("Token validated successfully using scheme: {Scheme}", scheme);
                return new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
            }

            _logger.LogWarning(
                "Token validation failed for scheme {Scheme}. Error: {Error}. Exception details: {@Exception}",
                scheme,
                tokenValidationResult.Exception?.Message,
                tokenValidationResult.Exception);

            return null;
        }
        catch (Exception ex)
        {
            // If any exception occurs during validation, we log it and return null
            // This is to ensure that any unexpected errors in token validation are treated as validation failures
            _logger.LogWarning(
                "Token validation failed for scheme {Scheme}. Error: {Error}. Exception details: {@Exception}",
                scheme,
                ex.Message,
                ex);
            return null;
        }
    }

    /// <summary>
    /// Attempts to update the token validation parameters with the latest OpenID Connect configuration.
    /// </summary>
    /// <param name="jwtBearerOptions">The JWT bearer options.</param>
    /// <param name="validationParameters">The token validation parameters to update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method is crucial for supporting dynamic OIDC configuration and key rotation.
    /// It's particularly relevant when using identity providers like IdentityServer.
    /// This approach ensures that the application always uses the most up-to-date signing keys
    /// without requiring a restart or manual configuration update.
    /// </remarks>
    private async Task TryUpdateValidationParametersAsync(
        JwtBearerOptions jwtBearerOptions,
        TokenValidationParameters validationParameters,
        CancellationToken cancellationToken)
    {
        if (jwtBearerOptions.ConfigurationManager != null)
        {
            _logger.LogDebug("Retrieving OpenID Connect configuration");
            try
            {
                // Asynchronously retrieve the OpenID Connect configuration
                var config = await jwtBearerOptions.ConfigurationManager
                    .GetConfigurationAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Update signing keys
                validationParameters.IssuerSigningKeys = config.SigningKeys;

                // Update issuer if validation is enabled
                if (validationParameters.ValidateIssuer)
                {
                    validationParameters.ValidIssuer = config.Issuer;
                }

                _logger.LogInformation("OpenID Connect configuration retrieved and validation parameters updated");
                _logger.LogDebug("Updated configuration - Keys: {KeyCount}, Issuer: {Issuer}",
                    validationParameters.IssuerSigningKeys?.Count() ?? 0,
                    validationParameters.ValidIssuer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve OpenID Connect configuration");
                throw;
            }
        }
        else
        {
            _logger.LogDebug("Skipping OpenID Connect configuration retrieval: ConfigurationManager not available");
        }
    }

    /// <summary>
    /// Checks if JWT Bearer authentication is explicitly configured.
    /// </summary>
    /// <returns>true if JWT Bearer authentication is explicitly configured; otherwise, false.</returns>
    /// <remarks>
    /// This method checks for the presence of the JWT Bearer authentication scheme.
    /// The presence of this scheme is a reliable indicator that JWT Bearer authentication has been configured.
    /// This approach is chosen because:
    /// 1. It's more reliable than checking specific option values, which might have default values even when not explicitly set.
    /// 2. It's simpler and faster than comparing multiple option values.
    /// 3. It directly reflects whether the AddJwtBearer() method has been called in the application's startup configuration.
    /// The result is cached to improve performance for subsequent calls.
    /// </remarks>
    private async Task<bool?> IsJwtBearerConfiguredAsync()
    {
        bool? isJwtBearerConfigured;

        if (_schemeProvider == null)
        {
            _logger.LogInformation("Authentication scheme provider is null, considering JWT Bearer as not configured");
            return false;
        }

        try
        {
            // Check if any of the configured schemes exist
            var configuredSchemes = 0;
            foreach (var scheme in _authenticationSchemes)
            {
                if (await _schemeProvider.GetSchemeAsync(scheme).ConfigureAwait(false) != null)
                {
                    configuredSchemes++;
                }
            }

            isJwtBearerConfigured = configuredSchemes > 0;

            _logger.LogInformation(
                "Found {ConfiguredCount} configured authentication schemes out of {TotalCount} specified schemes",
                configuredSchemes,
                _authenticationSchemes.Count);

            var allSchemes = await _schemeProvider.GetAllSchemesAsync().ConfigureAwait(false);

            _logger.LogDebug("All authentication schemes: {@Schemes}", allSchemes.Select(s => s.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking JWT Bearer configuration");
            // An exception occurred, so we don't know if JWT Bearer is configured
            isJwtBearerConfigured = null;
        }

        return isJwtBearerConfigured;
    }
}
