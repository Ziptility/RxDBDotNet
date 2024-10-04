// src\RxDBDotNet\Security\SocketConnectPayload.cs
namespace RxDBDotNet.Security;

/// <summary>
/// Represents the payload for a socket connection, containing the necessary authorization information.
/// </summary>
public class SocketConnectPayload
{
    /// <summary>
    /// Gets the headers required for the socket connection, including authorization information.
    /// </summary>
    /// <value>
    /// An instance of the <see cref="Headers"/> class containing the necessary authorization details.
    /// </value>
    public required Headers Headers { get; init; }
}
