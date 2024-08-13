﻿using RxDBDotNet.Documents;
using System.ComponentModel.DataAnnotations;
using LiveDocs.GraphQLApi.Validations;

namespace LiveDocs.GraphQLApi.Models.Replication;

/// <summary>
///     Base record for a document that is replicated via RxDBDotNet.
/// </summary>
public abstract record ReplicatedDocument : IReplicatedDocument
{
    private readonly List<string>? _topics;

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
    public required DateTimeOffset UpdatedAt { get; init; }

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
                          || (_topics != null && other._topics != null && _topics.SequenceEqual(other._topics));


        return topicsEqual
               && Id.Equals(other.Id)
               && IsDeleted == other.IsDeleted
               && UpdatedAt.Equals(other.UpdatedAt);
    }

    public override int GetHashCode()
    {
        // Calculate a combined hash code for the _topics collection.
        // We need to calculate this hash code in a way that aligns with the SequenceEqual comparison
        // used in Equals. SequenceEqual checks for element-wise equality between two collections,
        // so we must also ensure that the hash code reflects the individual elements of the _topics collection.

        var topicsHash = _topics != null
            // If _topics is not null, we iterate through each element in the collection.
            // The Aggregate function is used to combine the hash codes of all elements in the collection.
            // HashCode.Combine is used here to create a cumulative hash code that accounts for each element's hash code.
            // If an element is null, we use 0 as its hash code to avoid null reference exceptions.
            ? _topics.Aggregate(0, (hash, topic) => HashCode.Combine(hash, topic?.GetHashCode() ?? 0))

            // If _topics is null, we use 0 as the hash code, which matches the behavior in Equals
            // where null collections are considered equal.
            : 0;

        return HashCode.Combine(topicsHash, Id, IsDeleted, UpdatedAt);
    }
}
