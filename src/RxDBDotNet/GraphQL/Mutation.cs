using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

public class Mutation<TDocument> where TDocument : class, IReplicatedDocument
{
    [GraphQLName("pushDocument")]
    public Task<List<TDocument>> PushDocuments(List<PushDocumentRequest<TDocument>> documents, [Service] ReplicationResolvers<TDocument, DbContext> resolvers)
        => resolvers.PushDocuments(documents);
}
