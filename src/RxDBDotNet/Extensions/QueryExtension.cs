using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Repositories;
using RxDBDotNet.Resolvers;
using static RxDBDotNet.Extensions.DocumentExtensions;

namespace RxDBDotNet.Extensions;

#pragma warning disable CA1812
internal sealed class QueryExtension<TDocument> : ObjectTypeExtension
#pragma warning restore CA1812
    where TDocument : class, IReplicatedDocument
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        var graphQLTypeName = GetGraphQLTypeName<TDocument>();
        var pullDocumentsName = $"pull{graphQLTypeName}";
        var checkpointInputTypeName = $"{graphQLTypeName}InputCheckpoint";

        descriptor.Name("Query")
            .Field(pullDocumentsName)
            .UseFiltering<TDocument>()
            .Type<NonNullType<ObjectType<DocumentPullBulk<TDocument>>>>()
            .Argument("checkpoint", a => a.Type(checkpointInputTypeName)
                .Description($"The last known checkpoint for {graphQLTypeName} replication."))
            .Argument("limit", a => a.Type<NonNullType<IntType>>()
                .Description($"The maximum number of {graphQLTypeName} documents to return."))
            .Description(
                $"Pulls {graphQLTypeName} documents from the server based on the given checkpoint, limit, optional filters, and projections")
            .Resolve(context =>
            {
                var queryResolver = context.Resolver<QueryResolver<TDocument>>();
                var checkpoint = context.ArgumentValue<Checkpoint?>("checkpoint");
                var limit = context.ArgumentValue<int>("limit");
                var repository = context.Service<IDocumentRepository<TDocument>>();
                var cancellationToken = context.RequestAborted;

                return queryResolver.PullDocumentsAsync(checkpoint, limit, repository, context,
                    cancellationToken);
            });
    }
}
