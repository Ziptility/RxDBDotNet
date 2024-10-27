// src\RxDBDotNet\Extensions\DocumentExtensions.cs
using System.Reflection;
using HotChocolate;
using RxDBDotNet.Documents;

namespace RxDBDotNet.Extensions;

public static class DocumentExtensions
{
    /// <summary>
    /// Retrieves the GraphQL type name for a specified document type.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document, which must implement <see cref="IReplicatedDocument"/>.</typeparam>
    /// <returns>
    /// The GraphQL type name specified by the <see cref="GraphQLNameAttribute"/> if present;
    /// otherwise, the name of the document type.
    /// </returns>
    public static string GetGraphQLTypeName<TDocument>() where TDocument : IReplicatedDocument
    {
        var attribute = typeof(TDocument).GetCustomAttribute<GraphQLNameAttribute>();

        return attribute?.Name ?? typeof(TDocument).Name;
    }
}
