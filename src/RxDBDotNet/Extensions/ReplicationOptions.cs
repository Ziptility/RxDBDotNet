using RxDBDotNet.Documents;
using RxDBDotNet.Security;

namespace RxDBDotNet.Extensions;

/// <summary>
/// Provides configuration options for replicating documents of type <typeparamref name="TDocument"/>.
/// </summary>
/// <typeparam name="TDocument">
/// The type of document to be replicated, which must implement <see cref="IDocument"/>.
/// </typeparam>
public sealed class ReplicationOptions<TDocument>
    where TDocument : IDocument
{
    /// <summary>
    /// The security options for replicating documents of type <typeparamref name="TDocument"/>.
    /// </summary>
    public SecurityOptions<TDocument> Security { get; } = new();

    /// <summary>
    /// The list of error types that can occur when pushing changes for documents of type <typeparamref name="TDocument"/>.
    /// See https://chillicream.com/docs/hotchocolate/v13/defining-a-schema/mutations/#errors for more information.
    /// </summary>
    public List<Type> Errors { get; } = [];
}
