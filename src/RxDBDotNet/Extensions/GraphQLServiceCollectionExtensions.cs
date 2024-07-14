using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using RxDBDotNet.Documents;
using RxDBDotNet.GraphQL;
using RxDBDotNet.Models;

namespace RxDBDotNet.Extensions;

public static class GraphQLServiceCollectionExtensions
{
    public static IRequestExecutorBuilder AddReplicationSupport<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        return builder
            .ConfigureDynamicTypes<TDocument>();
    }

    public static IRequestExecutorBuilder ConfigureDynamicTypes<TDocument>(this IRequestExecutorBuilder builder)
        where TDocument : class, IReplicatedDocument
    {
        var documentTypeName = typeof(TDocument).Name;
        var checkpointInputTypeName = $"{documentTypeName}InputCheckpoint";
        var pullBulkTypeName = $"{documentTypeName}PullBulk";

        return builder
                .AddDocumentTypeExtensions<TDocument>(checkpointInputTypeName)
                .AddType(new InputObjectType<Checkpoint>(d =>
                {
                    d.Name(checkpointInputTypeName);
                    d.Field(f => f.UpdatedAt).Type<DateTimeType>();
                    d.Field(f => f.LastDocumentId).Type<IdType>();
                }))
                .AddType(new ObjectType<PullDocumentsResult<TDocument>>(d => d.Name(pullBulkTypeName)));
    }

    private static IRequestExecutorBuilder AddDocumentTypeExtensions<TDocument>(this IRequestExecutorBuilder builder, string checkpointInputTypeName)
        where TDocument : class, IReplicatedDocument
    {
        var documentTypeName = typeof(TDocument).Name;
        var pullDocumentsName = $"pull{documentTypeName}";

        builder.AddType(new ObjectType<QueryExtension<TDocument>>(descriptor =>
        {
            descriptor.Name("Query");
            descriptor.Field(t => t.PullDocuments(default, default))
                .Name(pullDocumentsName)
                .Argument("checkpoint", a => a.Type(checkpointInputTypeName))
                .Argument("limit", a => a.Type<NonNullType<IntType>>());
        }));

        return builder;
    }
}
