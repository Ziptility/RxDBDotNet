// src\RxDBDotNet\Extensions\DocumentExtensions.cs
using System.Reflection;
using HotChocolate;
using RxDBDotNet.Documents;

namespace RxDBDotNet.Extensions;

public static class DocumentExtensions
{
    public static string GetGraphQLTypeName<TDocument>() where TDocument : IReplicatedDocument
    {
        var attribute = typeof(TDocument).GetCustomAttribute<GraphQLNameAttribute>();

        return attribute?.Name ?? typeof(TDocument).Name;
    }
}
