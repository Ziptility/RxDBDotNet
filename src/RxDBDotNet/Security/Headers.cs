// src\RxDBDotNet\Security\Headers.cs
namespace RxDBDotNet.Security;

public sealed class Headers
{
    /// <summary>
    /// Gets the authorization token required for establishing a WebSocket connection.
    /// </summary>
    /// <value>
    /// A string representing the authorization token, typically in the form of a JWT.
    /// </value>
    public required string Authorization { get; init; }
}
