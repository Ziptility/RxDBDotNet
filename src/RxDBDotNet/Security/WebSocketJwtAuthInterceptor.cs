using System.Security.Claims;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
public class WebSocketJwtAuthInterceptor : DefaultSocketSessionInterceptor
{
    private const string BearerPrefix = "Bearer ";
    private readonly IAuthenticationSchemeProvider? _schemeProvider;
    private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptionsMonitor;
    private readonly ILogger<WebSocketJwtAuthInterceptor> _logger;
    private bool? _isJwtBearerConfigured;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketJwtAuthInterceptor"/> class.
    /// </summary>
    /// <param name="schemeProvider">The authentication scheme provider.</param>
    /// <param name="jwtOptionsMonitor">The options monitor for JWT bearer token validation.</param>
    /// <param name="logger">The logger for this middleware.</param>
    /// <remarks>
    /// We inject IAuthenticationSchemeProvider and IOptionsMonitor&lt;JwtBearerOptions&gt; for the following reasons:
    /// 1. IAuthenticationSchemeProvider allows us to check if JWT Bearer authentication is configured.
    /// 2. IOptionsMonitor&lt;JwtBearerOptions&gt; provides access to the JWT configuration for token validation.
    /// This approach allows the middleware to work correctly whether authentication is configured or not.
    /// </remarks>
    public WebSocketJwtAuthInterceptor(
        IAuthenticationSchemeProvider? schemeProvider,
        IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor,
        ILogger<WebSocketJwtAuthInterceptor> logger)
    {
        _schemeProvider = schemeProvider;
        _jwtOptionsMonitor = jwtOptionsMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Called when a new WebSocket connection is being established.
    /// This method handles the ConnectionInit message as per the graphql-transport-ws protocol.
    /// It validates the JWT token in the connection payload and sets up the ClaimsPrincipal for authenticated connections.
    /// If JWT authentication is not configured, it allows all connections.
    /// </summary>
    /// <param name="session">The socket session for the connection.</param>
    /// <param name="connectionInitMessage">The payload of the ConnectionInit message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="session"/> or <paramref name="connectionInitMessage"/> is null.</exception>
    /// <returns>A <see cref="ConnectionStatus"/> indicating whether the connection was accepted or rejected.</returns>
    /// <remarks>
    /// This method follows these steps:
    /// 1. Check if JWT Bearer authentication is configured.
    /// 2. If not configured, accept all connections (allowing for non-authenticated setups).
    /// 3. If configured, validate the JWT token from the ConnectionInit message payload.
    /// 4. Set up the ClaimsPrincipal for authenticated connections.
    /// 5. If authentication fails, reject the connection with a 4403: Forbidden status.
    /// This approach ensures that the middleware works in both authenticated and non-authenticated scenarios,
    /// providing flexibility for different application setups while adhering to the graphql-transport-ws protocol.
    /// </remarks>
    public override async ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(connectionInitMessage);

        try
        {
            // Check if JWT Bearer authentication is configured
            // This allows the middleware to work in both authenticated and non-authenticated setups
            if (!await IsJwtBearerConfiguredAsync().ConfigureAwait(false))
            {
                // If JWT Bearer is not configured, we accept all connections
                // This is crucial for supporting non-authenticated scenarios
                return ConnectionStatus.Accept();
            }

            // JWT Bearer is configured, so we proceed with token validation
            var token = ExtractToken(connectionInitMessage);
            if (string.IsNullOrEmpty(token))
            {
                return RejectConnection("No valid authorization token provided.");
            }

            // Validate the token
            var claimsPrincipal = await ValidateTokenAsync(token, cancellationToken).ConfigureAwait(false);
            if (claimsPrincipal == null)
            {
                return RejectConnection("Invalid authorization token.");
            }

            // If the token is valid, set the ClaimsPrincipal on the HttpContext
            // This allows the rest of the application to access the authenticated user's claims
            session.Connection.HttpContext.User = claimsPrincipal;
            return ConnectionStatus.Accept();
        }
        catch (Exception ex)
        {
            // Log the error and reject the connection
            // This ensures that we don't accidentally allow unauthorized access in case of errors
            _logger.LogError(ex, "An error occurred during WebSocket connection authentication.");
            return RejectConnection("An error occurred during authentication.");
        }
    }

    /// <summary>
    /// Extracts the JWT token from the connection initialization message.
    /// </summary>
    /// <param name="connectionInitMessage">The connection initialization message.</param>
    /// <returns>The extracted JWT token, or null if no valid token is found.</returns>
    private static string? ExtractToken(IOperationMessagePayload connectionInitMessage)
    {
        var connectPayload = connectionInitMessage.As<SocketConnectPayload>();
        var authorizationHeader = connectPayload?.Headers.Authorization;

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
    private static ConnectionStatus RejectConnection(string reason)
    {
        return ConnectionStatus.Reject("4403: Forbidden", new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            { "reason", reason },
        });
    }

    /// <summary>
    /// Validates the provided JWT token using the configured JWT bearer options.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
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
    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken)
    {
        // Retrieve the JWT Bearer options. These options are configured when setting up JWT authentication
        var jwtBearerOptions = _jwtOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        // Get the token handler from the options. This is typically a JwtSecurityTokenHandler
        var tokenHandler = jwtBearerOptions.TokenHandlers.Single();

        // Get the token validation parameters from the options
        var validationParameters = jwtBearerOptions.TokenValidationParameters;

        try
        {
            await UpdateValidationParametersAsync(jwtBearerOptions, validationParameters, cancellationToken)
                .ConfigureAwait(false);

            // Validate the token using the configured parameters
            // This step performs the actual cryptographic verification of the token
            var tokenValidationResult = await tokenHandler
                .ValidateTokenAsync(token, validationParameters)
                .ConfigureAwait(false);

            return tokenValidationResult.IsValid ? new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity) : null;
        }
        catch (Exception ex)
        {
            // If any exception occurs during validation, we log it and return null
            // This is to ensure that any unexpected errors in token validation are treated as validation failures
            _logger.LogWarning(ex, "Token validation failed.");
            return null;
        }
    }

    /// <summary>
    /// Updates the token validation parameters with the latest OpenID Connect configuration if necessary.
    /// </summary>
    /// <param name="jwtBearerOptions">The JWT bearer options.</param>
    /// <param name="validationParameters">The token validation parameters to update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <remarks>
    /// This method is crucial for supporting dynamic OIDC configuration and key rotation.
    /// It's particularly relevant when using identity providers like IdentityServer.
    /// This approach ensures that the application always uses the most up-to-date signing keys
    /// without requiring a restart or manual configuration update.
    /// </remarks>
    private static async Task UpdateValidationParametersAsync(
        JwtBearerOptions jwtBearerOptions,
        TokenValidationParameters validationParameters,
        CancellationToken cancellationToken)
    {
        // Check if we need to retrieve the OpenID Connect configuration
        // This is necessary when:
        // 1. No issuer signing keys are explicitly configured
        // 2. A configuration manager is available (typically set up when using OIDC discovery)
        if (validationParameters.IssuerSigningKeys.IsNullOrEmpty() && jwtBearerOptions.ConfigurationManager != null)
        {
            // Asynchronously retrieve the OpenID Connect configuration
            // This typically involves a network call to the OIDC provider's discovery endpoint
            var config = await jwtBearerOptions.ConfigurationManager
                .GetConfigurationAsync(cancellationToken)
                .ConfigureAwait(false);

            // Update the validation parameters with the fetched signing keys
            // This ensures we're using the most recent keys for token validation
            validationParameters.IssuerSigningKeys = config.SigningKeys;
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
    private async Task<bool> IsJwtBearerConfiguredAsync()
    {
        if (_isJwtBearerConfigured.HasValue)
        {
            return _isJwtBearerConfigured.Value;
        }

        if (_schemeProvider == null)
        {
            _isJwtBearerConfigured = false;
            return false;
        }

        var scheme = await _schemeProvider.GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
        _isJwtBearerConfigured = scheme != null;
        return _isJwtBearerConfigured.Value;
    }
}
