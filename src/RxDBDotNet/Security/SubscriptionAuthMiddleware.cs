using System.Security.Claims;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

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
public class SubscriptionAuthMiddleware : DefaultSocketSessionInterceptor
{
    private readonly IAuthenticationSchemeProvider? _schemeProvider;
    private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptionsMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionAuthMiddleware"/> class.
    /// </summary>
    /// <param name="schemeProvider">The authentication scheme provider.</param>
    /// <param name="jwtOptionsMonitor">The options monitor for JWT bearer token validation.</param>
    /// <remarks>
    /// We inject both IAuthenticationSchemeProvider and IOptionsMonitor&lt;JwtBearerOptions&gt; for the following reasons:
    /// 1. IAuthenticationSchemeProvider allows us to check if JWT Bearer authentication is configured.
    /// 2. IOptionsMonitor&lt;JwtBearerOptions&gt; provides access to the JWT configuration for token validation.
    /// This approach allows the middleware to work correctly whether authentication is configured or not.
    /// </remarks>
    public SubscriptionAuthMiddleware(
        IAuthenticationSchemeProvider? schemeProvider,
        IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor)
    {
        _schemeProvider = schemeProvider;
        _jwtOptionsMonitor = jwtOptionsMonitor;
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
            var connectPayload = connectionInitMessage.As<SocketConnectPayload>();
            var authorizationHeader = connectPayload?.Authorization;

            // Ensure the Authorization header is present and in the correct format
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // As per the protocol, we reject the connection with a 4403: Forbidden status
                return RejectConnection();
            }

            // Extract the token from the Authorization header
            var token = authorizationHeader["Bearer ".Length..].Trim();

            // Validate the token
            var claimsPrincipal = await ValidateTokenAsync(token).ConfigureAwait(false);

            if (claimsPrincipal != null)
            {
                // If the token is valid, set the ClaimsPrincipal on the HttpContext
                // This allows the rest of the application to access the authenticated user's claims
                session.Connection.HttpContext.User = claimsPrincipal;
                return ConnectionStatus.Accept();
            }

            // If the token is invalid, reject the connection with a 4403: Forbidden status
            return RejectConnection();
        }
        catch
        {
            // If any unexpected error occurs during the process, reject the connection
            // This ensures that we don't accidentally allow unauthorized access in case of errors
            return RejectConnection();
        }
    }

    private static ConnectionStatus RejectConnection()
    {
        return ConnectionStatus.Reject("4403: Forbidden", new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            { "reason", "Authentication failed" },
        });
    }

    /// <summary>
    /// Validates the provided JWT token using the configured JWT bearer options.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>
    /// A <see cref="ClaimsPrincipal"/> if the token is valid and a non-null principal was created; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method uses the same validation parameters as configured in <c>AddJwtBearer()</c>,
    /// ensuring consistency between HTTP and WebSocket authentication.
    /// The method is designed to handle exceptions during token validation, returning null for any validation failure.
    /// This approach allows the calling method to easily distinguish between valid and invalid tokens.
    /// </remarks>
    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        // Retrieve the JWT Bearer options. These options are configured when setting up JWT authentication
        var jwtBearerOptions = _jwtOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        // Get the token handler from the options. This is typically a JwtSecurityTokenHandler
        var tokenHandler = jwtBearerOptions.TokenHandlers.Single();

        // Get the token validation parameters from the options
        var validationParameters = jwtBearerOptions.TokenValidationParameters;

        try
        {
            // Attempt to validate the token
            var tokenValidationResult = await tokenHandler
                .ValidateTokenAsync(token, validationParameters)
                .ConfigureAwait(false);

            // If the token is valid, create and return a new ClaimsPrincipal
            if (tokenValidationResult.IsValid)
            {
                return new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
            }
        }
        catch (Exception)
        {
            // If any exception occurs during validation, we catch it and return null
            // This is to ensure that any unexpected errors in token validation are treated as validation failures
            return null;
        }

        // If we reach here, the token was invalid, so we return null
        return null;
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
    /// </remarks>
    private async Task<bool> IsJwtBearerConfiguredAsync()
    {
        // Attempt to retrieve the JWT Bearer authentication scheme
        if (_schemeProvider != null)
        {
            var scheme = await _schemeProvider.GetSchemeAsync(JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);

            // If the scheme is not null, it means JWT Bearer authentication has been configured
            return scheme != null;
        }

        // if _schemeProvider is null, we assume that JWT Bearer authentication is not configured
        return false;
    }
}
