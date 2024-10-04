// example/LiveDocs.GraphQLApi/Services/WorkspaceService.cs

using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using Microsoft.EntityFrameworkCore;
using RT.Comb;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Services;

public class WorkspaceService : DocumentService<Workspace, ReplicatedWorkspace>
{
    private readonly LiveDocsDbContext _dbContext;
    public WorkspaceService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher) : base(dbContext, eventPublisher)
    {
        _dbContext = dbContext;
    }

    public override IQueryable<ReplicatedWorkspace> GetQueryableDocuments()
    {
        return _dbContext.Set<Workspace>().AsNoTracking().Select(workspace => new ReplicatedWorkspace
        {
            Id = workspace.ReplicatedDocumentId,
            Name = workspace.Name,
            IsDeleted = workspace.IsDeleted,
            UpdatedAt = workspace.UpdatedAt,
#pragma warning disable RCS1077 // Optimize LINQ method call // EF Core cannot translate the optimized LINQ method call
            Topics = workspace.Topics.Select(t => t.Name).ToList(),
#pragma warning restore RCS1077 // Optimize LINQ method call
        });
    }

    protected override Task<Workspace> GetEntityByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return _dbContext.Workspaces
            .SingleAsync(ld => ld.ReplicatedDocumentId == documentId, cancellationToken);
    }

    protected override ReplicatedWorkspace MapToDocument(Workspace entityToMap)
    {
        ArgumentNullException.ThrowIfNull(entityToMap);

        return new ReplicatedWorkspace
        {
            Id = entityToMap.ReplicatedDocumentId,
            Name = entityToMap.Name,
            IsDeleted = entityToMap.IsDeleted,
            UpdatedAt = entityToMap.UpdatedAt,
            Topics = entityToMap.Topics.ConvertAll(t => t.Name),
        };
    }

    protected override Workspace Update(ReplicatedWorkspace updatedDocument, Workspace entityToUpdate)
    {
        ArgumentNullException.ThrowIfNull(updatedDocument);
        ArgumentNullException.ThrowIfNull(entityToUpdate);

        entityToUpdate.Name = updatedDocument.Name;
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;
        entityToUpdate.Topics.Clear();
        if (updatedDocument.Topics != null)
        {
            entityToUpdate.Topics.AddRange(updatedDocument.Topics.ConvertAll(t => new Topic { Name = t }));
        }

        return entityToUpdate;
    }

    protected override Task<Workspace> CreateAsync(ReplicatedWorkspace newDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(newDocument);

        return Task.FromResult(new Workspace
        {
            Id = Provider.Sql.Create(),
            ReplicatedDocumentId = newDocument.Id,
            Name = newDocument.Name,
            IsDeleted = false,
            UpdatedAt = newDocument.UpdatedAt,
            Topics = newDocument.Topics?.ConvertAll(t => new Topic { Name = t }) ?? [],
        });
    }
}
