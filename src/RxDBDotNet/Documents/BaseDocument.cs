namespace RxDBDotNet.Documents;

/// <summary>
/// Provides a base implementation of the <see cref="IReplicatedDocument"/> interface.
/// This class serves as a base for all documents that need to be replicated using the RxDB protocol,
/// ensuring consistent implementation of common properties such as <see cref="Id"/>,
/// <see cref="UpdatedAt"/>, and <see cref="IsDeleted"/>.
/// </summary>
public abstract class BaseDocument : IReplicatedDocument
{
    /// <inheritdoc />
    public required Guid Id { get; init; }

    /// <inheritdoc />
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <inheritdoc />
    public required bool IsDeleted { get; init; }
}
