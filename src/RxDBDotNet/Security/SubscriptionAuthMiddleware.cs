using System.Security.Claims;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace RxDBDotNet.Security;

/// <summary>
/// Middleware for authenticating WebSocket connections in GraphQL subscriptions.
/// This middleware validates JWT tokens sent in the connection payload and sets up the ClaimsPrincipal for authenticated connections.
/// If JWT authentication is not configured, it allows all connections.
/// </summary>
/// <remarks>
/// This middleware should be registered with the GraphQL server using AddSocketSessionInterceptor&lt;SubscriptionAuthMiddleware&gt;().
/// It uses the same JWT configuration as set up in AddJwtBearer() for consistency across HTTP and WebSocket connections, if available.
/// </remarks>
public class SubscriptionAuthMiddleware : DefaultSocketSessionInterceptor
{
    private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptionsMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionAuthMiddleware"/> class.
    /// </summary>
    /// <param name="jwtOptionsMonitor">The options monitor for JWT bearer token validation.</param>
    public SubscriptionAuthMiddleware(IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor)
    {
        _jwtOptionsMonitor = jwtOptionsMonitor;
    }

    /// <summary>
    /// Called when a new WebSocket connection is being established.
    /// This method validates the JWT token in the connection payload and sets up the ClaimsPrincipal for authenticated connections.
    /// If JWT authentication is not configured, it allows all connections.
    /// </summary>
    /// <param name="session">The socket session for the connection.</param>
    /// <param name="connectionInitMessage">The payload of the connection message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="session"/> or <paramref name="connectionInitMessage"/> is null.</exception>
    /// <returns>A <see cref="ConnectionStatus"/> indicating whether the connection was accepted or rejected.</returns>
    public override async ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(connectionInitMessage);

        try
        {
            // If JWT options are not configured, allow all connections
            if (!IsJwtConfigured())
            {
                return ConnectionStatus.Accept();
            }

            var connectPayload = connectionInitMessage.As<SocketConnectPayload>();
            var authorizationHeader = connectPayload?.Authorization;

            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return ConnectionStatus.Reject("Missing or invalid Authorization header");
            }

            var token = authorizationHeader["Bearer ".Length..].Trim();

            var claimsPrincipal = await ValidateTokenAsync(token).ConfigureAwait(false);

            if (claimsPrincipal != null)
            {
                session.Connection.HttpContext.User = claimsPrincipal;
                return ConnectionStatus.Accept();
            }

            return ConnectionStatus.Reject("Invalid token");
        }
        catch (Exception ex)
        {
            return ConnectionStatus.Reject(ex.Message);
        }
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
    /// If JWT options are not configured, this method always returns <c>null</c>.
    /// </remarks>
    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        if (!IsJwtConfigured())
        {
            return null;
        }

        var jwtBearerOptions = _jwtOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
        var tokenHandler = jwtBearerOptions.TokenHandlers.Single();
        var validationParameters = jwtBearerOptions.TokenValidationParameters;

        try
        {
            var tokenValidationResult = await tokenHandler
                .ValidateTokenAsync(token, validationParameters)
                .ConfigureAwait(false);
            if (tokenValidationResult.IsValid)
            {
                return new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    /// <summary>
    /// Checks if JWT authentication is configured.
    /// </summary>
    /// <returns>true if JWT authentication is configured; otherwise, false.</returns>
    private bool IsJwtConfigured()
    {
        try
        {
            // Accessing CurrentValue will throw if options are not configured
            _ = _jwtOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
            return true;
        }
        catch (OptionsValidationException)
        {
            return false;
        }
    }
}
