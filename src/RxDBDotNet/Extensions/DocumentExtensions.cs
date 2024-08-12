using RxDBDotNet.Documents;
using System.Reflection;

namespace RxDBDotNet.Extensions;

public static class DocumentExtensions
{
    public static string GetGraphQLTypeName<TDocument>() where TDocument : class, IReplicatedDocument
    {
        var attribute = typeof(TDocument).GetCustomAttribute<GraphQLNameAttribute>();

        return attribute?.Name ?? typeof(TDocument).Name;
    }
}
