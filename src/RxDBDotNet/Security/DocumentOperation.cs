namespace RxDBDotNet.Security;

/// <summary>
/// Represents an operation performed on a document.
/// </summary>
public record DocumentOperation
{
    /// <summary>
    /// The type of operation being performed.
    /// </summary>
    public required Operation Operation { get; init; }

    /// <summary>
    /// The type of document for which the operation is being performed.
    /// </summary>
    public required Type DocumentType { get; init; }
}
