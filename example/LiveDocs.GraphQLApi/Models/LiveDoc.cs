using RxDBDotNet.Documents;

namespace LiveDocs.GraphQLApi.Models;

/// <summary>
/// Represents a live, collaborative document within the LiveDocs system.
/// </summary>
/// <remarks>
/// <para>
/// A LiveDoc is the core entity in the LiveDocs collaborative editing platform. It encapsulates
/// the content and metadata of a single document that can be collaboratively edited in real-time
/// by multiple users within the same workspace. This class implements the IReplicatedDocument interface, 
/// enabling it to be efficiently synchronized across multiple clients and the server using the RxDB 
/// replication protocol.
/// </para>
/// <para>
/// Each LiveDoc has a unique identifier, content that can be edited, an owner, a workspace, and timestamps for
/// tracking updates. The IsDeleted property supports soft deletion, allowing for document recovery
/// and maintaining a consistent history of changes.
/// </para>
/// <para>
/// As an IReplicatedDocument, LiveDoc instances are automatically handled by the RxDBDotNet
/// replication system, ensuring real-time updates, conflict resolution, and offline support
/// across all connected clients within the same workspace.
/// </para>
/// </remarks>
/// <seealso cref="IReplicatedDocument"/>
public class LiveDoc : ReplicatedDocument
{
    /// <summary>
    /// Gets or sets the content of the live doc.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Gets or initializes the unique identifier of the live doc's owner.
    /// </summary>
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// Gets or initializes the unique identifier of the workspace to which the live doc belongs.
    /// </summary>
    public required Guid WorkspaceId { get; init; }
}
