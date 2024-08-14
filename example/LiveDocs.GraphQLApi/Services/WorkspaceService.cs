using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using RT.Comb;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Services;

public class WorkspaceService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher)
    : DocumentService<Workspace, ReplicatedWorkspace>(dbContext, eventPublisher)
{
    protected override Expression<Func<Workspace, ReplicatedWorkspace>> ProjectToDocument()
    {
        return workspace => new ReplicatedWorkspace
        {
            Id = workspace.ReplicatedDocumentId,
            Name = workspace.Name,
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = workspace.UpdatedAt,
#pragma warning disable RCS1077 // Optimize LINQ method call // EF Core cannot translate the optimized LINQ method call
            Topics = workspace.Topics == null ? null : workspace.Topics.Select(t => t.Name).ToList(),
#pragma warning restore RCS1077 // Optimize LINQ method call
        };
    }

    protected override Workspace Update(ReplicatedWorkspace updatedDocument, Workspace entityToUpdate)
    {
        ArgumentNullException.ThrowIfNull(updatedDocument);
        ArgumentNullException.ThrowIfNull(entityToUpdate);

        entityToUpdate.Name = updatedDocument.Name;
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;
        entityToUpdate.Topics = updatedDocument.Topics?.ConvertAll(t => new Topic
        {
            Name = t,
        });

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
            IsDeleted = false,
            UpdatedAt = newDocument.UpdatedAt,
            Topics = newDocument.Topics?.ConvertAll(t => new Topic { Name = t, }),
        };
    }
}
