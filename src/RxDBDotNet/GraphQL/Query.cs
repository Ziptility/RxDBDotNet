using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

public class Query<TDocument> where TDocument : class, IReplicatedDocument
{
    [GraphQLName("pullDocument")]
    public Task<PullDocumentsResult<TDocument>> PullDocuments(Checkpoint checkpoint, int limit, [Service] ReplicationResolvers<TDocument, DbContext> resolvers)
        => resolvers.PullDocuments(checkpoint, limit);
}
