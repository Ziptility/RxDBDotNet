using RxDBDotNet.Documents;
using RxDBDotNet.Security;

namespace RxDBDotNet.Extensions;

public sealed class ReplicationOptions<TDocument> where TDocument : class, IReplicatedDocument
{
    public SecurityOptions Security { get; } = new SecurityOptions();
}
