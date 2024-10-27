// src\RxDBDotNet\Configuration\DocumentOptions.cs

using System;
using System.Collections.Generic;
using RxDBDotNet.Documents;

namespace RxDBDotNet.Configuration;

/// <summary>
/// Provides configuration options for replicating documents of type <typeparamref name="TDocument"/>.
/// </summary>
/// <typeparam name="TDocument">
/// The type of document to be replicated, which must implement <see cref="IReplicatedDocument"/>.
/// </typeparam>
public sealed class DocumentOptions<TDocument>
    where TDocument : IReplicatedDocument
{
    /// <summary>
    /// Gets the document-level security options for documents of type <typeparamref name="TDocument"/>.
    /// These options control authorization and access control for specific document types.
    /// For global security settings like authentication schemes, see <see cref="ReplicationOptions.Security"/>.
    /// </summary>
    public DocumentSecurityOptions<TDocument> Security { get; set; } = new();

    /// <summary>
    /// Gets the list of error types that can occur when pushing changes for documents of type <typeparamref name="TDocument"/>.
    /// See <see href="https://chillicream.com/docs/hotchocolate/v13/defining-a-schema/mutations/#errors">Hot Chocolate Mutation Errors</see> for more information.
    /// </summary>
    /// <remarks>
    /// These error types are used to handle specific exceptions that may be thrown
    /// during the document replication process.
    /// </remarks>
    public List<Type> Errors { get; } = [];
}
