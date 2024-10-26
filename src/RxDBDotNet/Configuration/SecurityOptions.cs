// src\RxDBDotNet\Configuration\SecurityOptions.cs

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace RxDBDotNet.Configuration;

/// <summary>
/// Provides global security-related configuration options for RxDB replication.
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// Gets or sets the authentication scheme used for validating Subscription JWT tokens.
    /// If not specified, defaults to JwtBearerDefaults.AuthenticationScheme.
    /// </summary>
    /// <remarks>
    /// This is particularly useful when your application has multiple JWT authentication schemes
    /// configured and you want to specify which one should be used for WebSocket authentication.
    /// </remarks>
    public string SubscriptionAuthenticationScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;
}
