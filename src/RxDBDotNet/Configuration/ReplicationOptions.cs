// src\RxDBDotNet\Configuration\ReplicationOptions.cs

namespace RxDBDotNet.Configuration;

/// <summary>
/// Provides global configuration options for RxDB replication.
/// </summary>
public class ReplicationOptions
{
    /// <summary>
    /// Gets security-related configuration options.
    /// </summary>
    public SecurityOptions Security { get; } = new();
}
