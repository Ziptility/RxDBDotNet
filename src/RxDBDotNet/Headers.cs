namespace RxDBDotNet;

/// <summary>
/// Represents the authentication headers for streaming operations.
/// </summary>
public class Headers
{
    /// <summary>
    /// The authentication token.
    /// </summary>
    public required string AuthToken { get; set; }
}
