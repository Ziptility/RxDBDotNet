using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using Microsoft.EntityFrameworkCore;
using RT.Comb;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Services;

public class LiveDocService : DocumentService<LiveDoc, ReplicatedLiveDoc>
{
    private readonly LiveDocsDbContext _dbContext;

    public LiveDocService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher)
        : base(dbContext, eventPublisher)
    {
        _dbContext = dbContext;
    }

    protected override Expression<Func<LiveDoc, ReplicatedLiveDoc>> ProjectToDocument()
    {
        return liveDoc => new ReplicatedLiveDoc
        {
            Id = liveDoc.ReplicatedDocumentId,
            Content = liveDoc.Content,
            OwnerId = liveDoc.Owner!.ReplicatedDocumentId,
            WorkspaceId = liveDoc.Workspace!.ReplicatedDocumentId,
            IsDeleted = liveDoc.IsDeleted,
            UpdatedAt = liveDoc.UpdatedAt,
#pragma warning disable RCS1077 // Optimize LINQ method call // EF Core cannot translate the optimized LINQ method call
            Topics = liveDoc.Topics == null ? null : liveDoc.Topics.Select(t => t.Name).ToList(),
#pragma warning restore RCS1077 // Optimize LINQ method call
        };
    }

    protected override LiveDoc Update(ReplicatedLiveDoc updatedDocument, LiveDoc entityToUpdate)
    {
        ArgumentNullException.ThrowIfNull(updatedDocument);
        ArgumentNullException.ThrowIfNull(entityToUpdate);

        entityToUpdate.Content = updatedDocument.Content;
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;
        entityToUpdate.Topics = updatedDocument.Topics?.ConvertAll(t => new Topic
        {
            Name = t,
        });

        return entityToUpdate;
    }

    protected override async Task<LiveDoc> CreateAsync(ReplicatedLiveDoc newDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(newDocument);

        var workspacePk = await _dbContext.Workspaces
            .Where(w => w.ReplicatedDocumentId == newDocument.WorkspaceId)
            .Select(w => w.Id)
            .SingleAsync(cancellationToken);

        var ownerPk = await _dbContext.Users
            .Where(w => w.ReplicatedDocumentId == newDocument.OwnerId)
            .Select(w => w.Id)
            .SingleAsync(cancellationToken);

        return new LiveDoc
        {
            Id = Provider.Sql.Create(),
            ReplicatedDocumentId = newDocument.Id,
            Content = newDocument.Content,
            OwnerId = ownerPk,
            WorkspaceId = workspacePk,
            IsDeleted = false,
            UpdatedAt = newDocument.UpdatedAt,
            Topics = newDocument.Topics?.ConvertAll(t => new Topic { Name = t }),
        };
    }
}
