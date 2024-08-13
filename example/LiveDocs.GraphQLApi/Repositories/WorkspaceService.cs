using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using RT.Comb;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Repositories;

public class WorkspaceService : DocumentService<Workspace, ReplicatedWorkspace>
{
    public WorkspaceService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher) : base(dbContext, eventPublisher)
    {
    }

    protected override Expression<Func<Workspace, ReplicatedWorkspace>> ProjectToDocument()
    {
        return workspace => new ReplicatedWorkspace
        {
            Id = workspace.ReplicatedDocumentId,
            Name = workspace.Name,
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = workspace.UpdatedAt,
            Topics = workspace.Topics,
        };
    }

    protected override Workspace Update(ReplicatedWorkspace updatedDocument, Workspace entityToUpdate)
    {
        ArgumentNullException.ThrowIfNull(updatedDocument);
        ArgumentNullException.ThrowIfNull(entityToUpdate);
        
        entityToUpdate.Name = updatedDocument.Name;
        entityToUpdate.Topics = updatedDocument.Topics;
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;

        return entityToUpdate;
    }

    protected override Workspace Create(ReplicatedWorkspace newDocument)
    {
        ArgumentNullException.ThrowIfNull(newDocument);
        
        return new Workspace
        {
            Id = Provider.Sql.Create(),
            ReplicatedDocumentId = newDocument.Id,
            Name = newDocument.Name,
            Topics = newDocument.Topics,
            IsDeleted = false,
            UpdatedAt = newDocument.UpdatedAt,
        };
    }
}
