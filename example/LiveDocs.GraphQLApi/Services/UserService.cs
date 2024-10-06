// example/LiveDocs.GraphQLApi/Services/UserService.cs

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Security;
using Microsoft.EntityFrameworkCore;
using RT.Comb;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Services;

public class UserService : DocumentService<User, ReplicatedUser>
{
    private readonly LiveDocsDbContext _dbContext;

    public UserService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher) : base(dbContext, eventPublisher)
    {
        _dbContext = dbContext;
    }

    public override IQueryable<ReplicatedUser> GetQueryableDocuments()
    {
        return _dbContext.Set<User>().AsNoTracking().Select(user => new ReplicatedUser
        {
            Id = user.ReplicatedDocumentId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            JwtAccessToken = user.JwtAccessToken,
            WorkspaceId = user.Workspace!.ReplicatedDocumentId,
            IsDeleted = user.IsDeleted,
            UpdatedAt = user.UpdatedAt,
#pragma warning disable RCS1077 // Optimize LINQ method call // EF Core cannot translate the optimized LINQ method call
            Topics = user.Topics.Select(t => t.Name).ToList(),
#pragma warning restore RCS1077 // Optimize LINQ method call
        });
    }

    protected override Task<User> GetEntityByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            // Include the workspace entity in the query
            // so that the document can be mapped to a ReplicatedUser
            .Include(ld => ld.Workspace)
            .SingleAsync(ld => ld.ReplicatedDocumentId == documentId, cancellationToken);
    }

    protected override ReplicatedUser MapToDocument(User entityToMap)
    {
        ArgumentNullException.ThrowIfNull(entityToMap);
        ArgumentNullException.ThrowIfNull(entityToMap.Workspace);

        return new ReplicatedUser
        {
            Id = entityToMap.ReplicatedDocumentId,
            FirstName = entityToMap.FirstName,
            LastName = entityToMap.LastName,
            Email = entityToMap.Email,
            Role = entityToMap.Role,
            JwtAccessToken = entityToMap.JwtAccessToken,
            WorkspaceId = entityToMap.Workspace.ReplicatedDocumentId,
            IsDeleted = entityToMap.IsDeleted,
            UpdatedAt = entityToMap.UpdatedAt,
            Topics = entityToMap.Topics.ConvertAll(t => t.Name),
        };
    }

    protected override User Update(ReplicatedUser updatedDocument, User entityToUpdate)
    {
        ArgumentNullException.ThrowIfNull(updatedDocument);
        ArgumentNullException.ThrowIfNull(entityToUpdate);

        entityToUpdate.FirstName = updatedDocument.FirstName;
        entityToUpdate.LastName = updatedDocument.LastName;
        entityToUpdate.Email = updatedDocument.Email;
        entityToUpdate.Role = updatedDocument.Role;
        entityToUpdate.JwtAccessToken = JwtUtil.GenerateJwtToken(updatedDocument);
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;
        entityToUpdate.Topics.Clear();
        if (updatedDocument.Topics != null)
        {
            entityToUpdate.Topics.AddRange(updatedDocument.Topics.ConvertAll(t => new Topic { Name = t }));
        }

        return entityToUpdate;
    }

    protected override async Task<User> CreateAsync(ReplicatedUser newDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(newDocument);

        var workspace = await _dbContext.Workspaces
            .Where(w => w.ReplicatedDocumentId == newDocument.WorkspaceId)
            .SingleAsync(cancellationToken);

        var jwtToken = JwtUtil.GenerateJwtToken(newDocument);

        return new User
        {
            Id = Provider.Sql.Create(),
            ReplicatedDocumentId = newDocument.Id,
            FirstName = newDocument.FirstName,
            LastName = newDocument.LastName,
            Email = newDocument.Email,
            Role = newDocument.Role,
            JwtAccessToken = jwtToken,
            UpdatedAt = newDocument.UpdatedAt,
            IsDeleted = false,
            WorkspaceId = workspace.Id,
            Workspace = workspace,
            Topics = newDocument.Topics?.ConvertAll(t => new Topic { Name = t }) ?? [],
        };
    }
}
