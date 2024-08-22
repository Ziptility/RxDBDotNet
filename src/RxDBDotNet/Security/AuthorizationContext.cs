namespace RxDBDotNet.Security;

/// <summary>
/// Represents the context in which an operation is being performed.
/// </summary>
public record AuthorizationContext
{
    /// <summary>
    /// The type of operation being performed.
    /// </summary>
    public required OperationType OperationType { get; init; }

    /// <summary>
    /// The type of document for which the operation is being performed.
    /// </summary>
    public required Type DocumentType { get; init; }
}
