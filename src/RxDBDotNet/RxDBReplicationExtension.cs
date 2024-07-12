using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Types;
using RxDBDotNet;

public class RxDBReplicationExtension<TEntity> : ObjectTypeExtension<TEntity>
    where TEntity : class, IReplicatedEntity
{
    protected override void Configure(IObjectTypeDescriptor<TEntity> descriptor)
    {
        descriptor.Field(t => RxDBReplicationExtension<TEntity>.ReplicationResolvers.PullData(default, default, default))
            .Name($"pull{typeof(TEntity).Name}")
            .Argument("checkpoint", a => a.Type<NonNullType<CheckpointInputType>>())
            .Argument("limit", a => a.Type<NonNullType<IntType>>())
            .Type<NonNullType<PullBulkType<TEntity>>>()
            .ResolveWith<ReplicationResolvers>(r => r.PullData(default, default, default));

        descriptor.Field(t => RxDBReplicationExtension<TEntity>.ReplicationResolvers.PushData(default, default))
            .Name($"push{typeof(TEntity).Name}")
            .Argument("rows", a => a.Type<NonNullType<ListType<NonNullType<PushRowInputType<TEntity>>>>>())
            .Type<NonNullType<ListType<NonNullType<EntityType<TEntity>>>>>()
            .ResolveWith<ReplicationResolvers>(r => r.PushData(default, default));
    }

    private class ReplicationResolvers
    {
        public async Task<PullBulk<TEntity>> PullData(Checkpoint checkpoint, int limit, [Service] ApplicationDbContext dbContext)
        {
            var documents = await dbContext.Set<TEntity>()
                                           .Where(e => EF.Property<float>(e, nameof(IReplicatedEntity.UpdatedAt)) > checkpoint.UpdatedAt)
                                           .OrderBy(e => EF.Property<float>(e, nameof(IReplicatedEntity.UpdatedAt)))
                                           .Take(limit)
                                           .ToListAsync();

            var lastUpdated = documents.Any() ? documents.Max(e => EF.Property<float>(e, nameof(IReplicatedEntity.UpdatedAt))) : checkpoint.UpdatedAt;

            return new PullBulk<TEntity>
            {
                Documents = documents,
                Checkpoint = new Checkpoint { Id = "lastCheckpoint", UpdatedAt = lastUpdated }
            };
        }

        public async Task<List<TEntity>> PushData(List<PushRow<TEntity>> rows, [Service] ApplicationDbContext dbContext)
        {
            var conflicts = new List<TEntity>();

            foreach (var row in rows)
            {
                var existing = await dbContext.Set<TEntity>().FindAsync(row.NewDocumentState.Id);
                if (existing != null)
                {
                    // Conflict resolution logic
                    var existingUpdatedAt = EF.Property<float>(existing, nameof(IReplicatedEntity.UpdatedAt));
                    if (row.AssumedMasterState == null || existingUpdatedAt != row.AssumedMasterState.UpdatedAt)
                    {
                        conflicts.Add(existing);
                    }
                    else
                    {
                        dbContext.Entry(existing).CurrentValues.SetValues(row.NewDocumentState);
                    }
                }
                else
                {
                    dbContext.Set<TEntity>().Add(row.NewDocumentState);
                }
            }

            await dbContext.SaveChangesAsync();
            return conflicts;
        }
    }
}
