namespace LiveDocs.GraphQLApi.Models;

/// <summary>
/// Represents errors that occur when a concurrency conflict is detected during document operations.
/// </summary>
[Serializable]
public class ConcurrencyException : Exception
{
    /// <summary>
    /// Gets the ID of the document that caused the concurrency conflict.
    /// </summary>
    public Guid DocumentId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    public ConcurrencyException() : base("A concurrency conflict occurred during a document operation.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConcurrencyException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a specified error message
    /// and the ID of the document that caused the concurrency conflict.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="documentId">The ID of the document that caused the concurrency conflict.</param>
    public ConcurrencyException(string message, Guid documentId) : base(message)
    {
        DocumentId = documentId;
    }

    /// <summary>
    /// Creates and returns a string representation of the current exception.
    /// </summary>
    /// <returns>A string representation of the current exception.</returns>
    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(DocumentId)}: {DocumentId}";
    }
}
