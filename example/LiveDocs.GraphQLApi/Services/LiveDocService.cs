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
            Topics = liveDoc.Topics.Select(t => t.Name).ToList(),
#pragma warning restore RCS1077 // Optimize LINQ method call
        };
    }

    protected override Task<LiveDoc> GetEntityByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return _dbContext.LiveDocs
            // Include the owner and workspace entities in the query
            // so that the document can be mapped to a ReplicatedLiveDoc
            .Include(ld => ld.Owner)
            .Include(ld => ld.Workspace)
            .SingleAsync(ld => ld.ReplicatedDocumentId == documentId, cancellationToken);
    }

    protected override ReplicatedLiveDoc MapToDocument(LiveDoc entityToMap)
    {
        ArgumentNullException.ThrowIfNull(entityToMap);
        ArgumentNullException.ThrowIfNull(entityToMap.Owner);
        ArgumentNullException.ThrowIfNull(entityToMap.Workspace);

        return new ReplicatedLiveDoc
        {
            Id = entityToMap.ReplicatedDocumentId,
            Content = entityToMap.Content,
            OwnerId = entityToMap.Owner.ReplicatedDocumentId,
            WorkspaceId = entityToMap.Workspace.ReplicatedDocumentId,
            IsDeleted = entityToMap.IsDeleted,
            UpdatedAt = entityToMap.UpdatedAt,
            Topics = entityToMap.Topics.ConvertAll(t => t.Name),
        };
    }

    protected override LiveDoc Update(ReplicatedLiveDoc updatedDocument, LiveDoc entityToUpdate)
    {
        ArgumentNullException.ThrowIfNull(updatedDocument);
        ArgumentNullException.ThrowIfNull(entityToUpdate);

        entityToUpdate.Content = updatedDocument.Content;
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;
        entityToUpdate.Topics.Clear();
        if (updatedDocument.Topics != null)
        {
            entityToUpdate.Topics.AddRange(updatedDocument.Topics.ConvertAll(t => new Topic { Name = t }));
        }
        return entityToUpdate;
    }

    protected override async Task<LiveDoc> CreateAsync(ReplicatedLiveDoc newDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(newDocument);

        var workspace = await _dbContext.Workspaces
            .Where(w => w.ReplicatedDocumentId == newDocument.WorkspaceId)
            .SingleAsync(cancellationToken);

        var owner = await _dbContext.Users
            .Where(w => w.ReplicatedDocumentId == newDocument.OwnerId)
            .SingleAsync(cancellationToken);

        return new LiveDoc
        {
            Id = Provider.Sql.Create(),
            ReplicatedDocumentId = newDocument.Id,
            Content = newDocument.Content,
            OwnerId = owner.Id,
            Owner = owner,
            WorkspaceId = workspace.Id,
            Workspace = workspace,
            UpdatedAt = newDocument.UpdatedAt,
            IsDeleted = false,
            Topics = newDocument.Topics?.ConvertAll(t => new Topic { Name = t }) ?? [],
        };
    }
}
