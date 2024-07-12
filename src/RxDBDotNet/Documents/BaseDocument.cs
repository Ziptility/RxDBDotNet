namespace RxDBDotNet.Documents;

/// <summary>
/// Provides a base implementation of the <see cref="IReplicatedDocument"/> interface.
/// This class serves as a base for all documents that need to be replicated using the RxDB protocol,
/// ensuring consistent implementation of common properties such as <see cref="DocumentId"/>,
/// <see cref="UpdatedAt"/>, and <see cref="IsDeleted"/>.
/// </summary>
/// <remarks>
/// The <see cref="BaseDocument"/> class exists to provide a consistent implementation of the
/// <see cref="IReplicatedDocument"/> interface, ensuring that all derived document classes
/// adhere to the same structure and behavior required for replication. This reduces redundancy
/// and potential errors by centralizing the implementation of common properties.
/// </remarks>
public abstract class BaseDocument : IReplicatedDocument
{
    /// <inheritdoc />
    public required Guid DocumentId { get; init; }

    /// <inheritdoc />
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <inheritdoc />
    public required bool IsDeleted { get; init; }
}
