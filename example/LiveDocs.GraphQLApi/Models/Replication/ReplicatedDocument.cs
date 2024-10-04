// example/LiveDocs.GraphQLApi/Models/Replication/ReplicatedDocument.cs

using System.ComponentModel.DataAnnotations;
using LiveDocs.GraphQLApi.Validations;
using RxDBDotNet.Documents;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
/// Base record for a document that is replicated via RxDBDotNet.
/// </summary>
public abstract record ReplicatedDocument : IReplicatedDocument
{
    private readonly List<string>? _topics;
    private DateTimeOffset _updatedAt;

    /// <inheritdoc />
    [Required]
    [NotDefault]
    public required Guid Id { get; init; }

    /// <inheritdoc />
    [Required]
    public required bool IsDeleted { get; init; }

    /// <inheritdoc />
    [Required]
    [NotDefault]
    public required DateTimeOffset UpdatedAt
    {
        get => _updatedAt;
        set =>
            // Strip microseconds by setting the ticks to zero, keeping only up to milliseconds.
            // Doing this because microseconds are not supported by Hot Chocolate's DateTime serializer.
            // Now Equals() and GetHashCode() will work correctly across roundtrips.
            _updatedAt = value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMillisecond));
    }

    /// <inheritdoc />
    [Length(1, 10)]
    public List<string>? Topics
    {
        get => _topics;
        init => _topics = value?.Select(topic => topic.Trim()).ToList();
    }

    public virtual bool Equals(ReplicatedDocument? other)
    {
        if (other is null || GetType() != other.GetType())
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Compare the _topics collections:
        // 1. If both are null, they are considered equal.
        // 2. If both are non-null, use SequenceEqual to compare each element in the collections.
        var topicsEqual = (_topics == null && other._topics == null)
                          || (_topics != null && other._topics != null && _topics.SequenceEqual(other._topics, StringComparer.Ordinal));

        return topicsEqual && Id.Equals(other.Id) && IsDeleted == other.IsDeleted && UpdatedAt.Equals(other.UpdatedAt);
    }

    public override int GetHashCode()
    {
        // Calculate a combined hash code for the _topics collection.
        var topicsHash = _topics?.Aggregate(0, (hash, topic) => HashCode.Combine(hash, topic.GetHashCode(StringComparison.Ordinal)))
                         ?? 0;

        return HashCode.Combine(topicsHash, Id, IsDeleted, UpdatedAt);
    }
}
