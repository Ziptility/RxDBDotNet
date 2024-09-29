using System.Reflection;
using RxDBDotNet.Documents;

namespace RxDBDotNet.Extensions;

public static class DocumentExtensions
{
    public static string GetGraphQLTypeName<TDocument>() where TDocument : IDocument
    {
        var attribute = typeof(TDocument).GetCustomAttribute<GraphQLNameAttribute>();

        return attribute?.Name ?? typeof(TDocument).Name;
    }
}
