namespace RxDBDotNet.Types;

/// <summary>
/// Defines the GraphQL object type for a PullBulk operation result.
/// </summary>
/// <typeparam name="TEntity">The type of entity being replicated.</typeparam>
public class PullBulkType<TEntity> : ObjectType<PullBulk<TEntity>> where TEntity : class, IReplicatedEntity
{
    protected override void Configure(IObjectTypeDescriptor<PullBulk<TEntity>> descriptor)
    {
        descriptor.Field(f => f.Documents)
            .Type<NonNullType<ListType<NonNullType<ObjectType<TEntity>>>>>();
        descriptor.Field(f => f.Checkpoint)
            .Type<NonNullType<CheckpointInputType>>();
    }
}
