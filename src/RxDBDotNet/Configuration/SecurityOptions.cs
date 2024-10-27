// src\RxDBDotNet\Configuration\SecurityOptions.cs

using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
namespace RxDBDotNet.Configuration;

/// <summary>
/// Provides global security-related configuration options for RxDB replication.
/// </summary>
public class SecurityOptions
{
    private readonly List<string> _subscriptionAuthenticationSchemes = [JwtBearerDefaults.AuthenticationScheme];

    /// <summary>
    /// Gets the authentication schemes used for validating Subscription JWT tokens.
    /// The default value is a list containing only JwtBearerDefaults.AuthenticationScheme.
    /// </summary>
    public IReadOnlyList<string> SubscriptionAuthenticationSchemes => _subscriptionAuthenticationSchemes;

    /// <summary>
    /// Adds an authentication scheme to be used for WebSocket authentication if not already added.
    /// </summary>
    /// <param name="scheme">The authentication scheme to add.</param>
    /// <returns>The current SecurityOptions instance for method chaining.</returns>
    public SecurityOptions TryAddSubscriptionAuthenticationScheme(string scheme)
    {
        if (!_subscriptionAuthenticationSchemes.Contains(scheme))
        {
            _subscriptionAuthenticationSchemes.Add(scheme);
        }
        return this;
    }
}
