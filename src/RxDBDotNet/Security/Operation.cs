using System.Text.Json.Serialization;

namespace RxDBDotNet.Security;

/// <summary>
/// Defines the types of operations that can be controlled in the RxDBDotNet security system.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Operation
{
    /// <summary>
    /// Represents the absence of any operation.
    /// </summary>
    [GraphQLName("None")]
    None = 0,

    /// <summary>
    /// Represents an operation that reads a replicated document.
    /// This includes pulls and subscriptions.
    /// </summary>
    [GraphQLName("Read")]
    Read = 1 << 0,

    /// <summary>
    /// Represents an operation that creates a replicated document.
    /// This includes pushes that contain a new document.
    /// </summary>
    [GraphQLName("Create")]
    Create = 1 << 1,

    /// <summary>
    /// Represents an operation that updates a replicated document.
    /// This includes pushes that contain an updated document.
    /// </summary>
    [GraphQLName("Update")]
    Update = 1 << 2,

    /// <summary>
    /// Represents an operation that deletes a replicated document.
    /// This includes pushes that contain an deleted document.
    /// </summary>
    [GraphQLName("Delete")]
    Delete = 1 << 3,

    /// <summary>
    /// Represents all possible operations including Read, Create, Update, and Delete.
    /// </summary>
    [GraphQLName("All")]
    All = Read | Create | Update | Delete,
}
