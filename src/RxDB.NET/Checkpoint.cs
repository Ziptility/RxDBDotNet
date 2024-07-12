namespace RxDB.NET;

/// <summary>
/// Represents a checkpoint used for replication.
/// </summary>
public class Checkpoint
{
    /// <summary>
    /// The unique identifier for the checkpoint.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The timestamp of the checkpoint.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
