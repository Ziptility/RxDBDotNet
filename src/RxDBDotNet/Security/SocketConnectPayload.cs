namespace RxDBDotNet.Security;

/// <summary>
/// Represents the payload for a socket connection, containing the necessary authorization information.
/// </summary>
public class SocketConnectPayload
{
    /// <summary>
    /// Gets the authorization token required for establishing a WebSocket connection.
    /// </summary>
    /// <value>
    /// A string representing the authorization token, typically in the form of a JWT.
    /// </value>
    public required string Authorization { get; init; }
}
