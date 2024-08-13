using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using RT.Comb;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Repositories;

public class LiveDocService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher)
    : DocumentService<LiveDoc, ReplicatedLiveDoc>(dbContext, eventPublisher)
{
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
            Topics = liveDoc.Topics,
        };
    }

    protected override LiveDoc Update(ReplicatedLiveDoc updatedDocument, LiveDoc entityToUpdate)
    {
        ArgumentNullException.ThrowIfNull(updatedDocument);
        ArgumentNullException.ThrowIfNull(entityToUpdate);

        entityToUpdate.Content = updatedDocument.Content;
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;
        entityToUpdate.Topics = updatedDocument.Topics;

        return entityToUpdate;
    }

    protected override LiveDoc Create(ReplicatedLiveDoc newDocument)
    {
        ArgumentNullException.ThrowIfNull(newDocument);

        return new LiveDoc
        {
            Id = Provider.Sql.Create(),
            ReplicatedDocumentId = newDocument.Id,
            Content = newDocument.Content,
            OwnerId = newDocument.OwnerId,
            WorkspaceId = newDocument.WorkspaceId,
            IsDeleted = false,
            UpdatedAt = newDocument.UpdatedAt,
            Topics = newDocument.Topics,
        };
    }
}
