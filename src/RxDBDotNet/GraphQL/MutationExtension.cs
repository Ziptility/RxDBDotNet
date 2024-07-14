using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Documents;
using RxDBDotNet.Models;
using RxDBDotNet.Resolvers;

namespace RxDBDotNet.GraphQL;

[ExtendObjectType("Mutation")]
public class MutationExtension<TDocument> where TDocument : class, IReplicatedDocument
{
    public Task<List<TDocument>> PushDocuments(
        List<PushDocumentRequest<TDocument>> documents,
        [Service] ReplicationResolvers<TDocument, DbContext> resolvers)
    {
        return resolvers.PushDocuments(documents);
    }
}
