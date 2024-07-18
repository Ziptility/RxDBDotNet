namespace RxDBDotNet.Models;

public sealed class Headers
{
    /// <summary>
    /// The JWT bearer token used to authenticate the subscription over websockets.
    /// </summary>
    public required string Authorization { get; init; }
}
