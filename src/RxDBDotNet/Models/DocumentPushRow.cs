using RxDBDotNet.Documents;

namespace RxDBDotNet.Models;

/// <summary>
///     Represents a document being pushed, including its assumed master state.
/// </summary>
/// <typeparam name="TDocument">The type of document being replicated.</typeparam>
public class DocumentPushRow<TDocument> where TDocument : class, IReplicatedDocument
{
    /// <summary>
    ///     <para>
    ///         The assumed state of the document on the master before the push.
    ///         This represents the state of the document as the client believes it exists on the server.
    ///         If this value is null, it indicates that the client is not aware of any existing state on the server
    ///         (e.g., the document is new or the client is unable to retrieve the current state).
    ///     </para>
    ///     <para>
    ///         The server will interpret a null value as indicating no prior state is assumed, and it will not check
    ///         for conflicts based on the previous state. This is commonly used for new documents being created.
    ///     </para>
    /// </summary>
    public required TDocument? AssumedMasterState { get; init; }

    /// <summary>
    ///     The new state of the document being pushed.
    ///     This represents the updated state of the document that the client wants to synchronize with the server.
    /// </summary>
    public required TDocument NewDocumentState { get; init; }
}
