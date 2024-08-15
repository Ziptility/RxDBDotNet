namespace RxDBDotNet.Security;

/// <summary>
/// Defines the types of access that can be controlled in the RxDBDotNet security system.
/// </summary>
[Flags]
public enum AccessType
{
    /// <summary>
    /// Represents no access rights.
    /// </summary>
    None = 0,

    /// <summary>
    /// Represents read access to documents. This includes operations like queries and subscriptions.
    /// </summary>
    Read = 1 << 0,

    /// <summary>
    /// Represents write access to documents. This includes operations like creating new documents and updating existing ones.
    /// </summary>
    Write = 1 << 1,

    /// <summary>
    /// Represents delete access to documents. This allows for the deletion of documents.
    /// </summary>
    Delete = 1 << 2,

    /// <summary>
    /// Represents both read and write access to documents.
    /// </summary>
    ReadWrite = Read | Write,

    /// <summary>
    /// Represents full access to documents, including read, write, and delete operations.
    /// </summary>
    All = Read | Write | Delete,
}
