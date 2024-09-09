using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
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

    protected override Expression<Func<User, ReplicatedUser>> ProjectToDocument()
    {
        return user => new ReplicatedUser
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
            Topics = user.Topics == null ? null : user.Topics.Select(t => t.Name).ToList(),
#pragma warning restore RCS1077 // Optimize LINQ method call
        };
    }

    protected override User Update(ReplicatedUser updatedDocument, User entityToUpdate)
    {
        ArgumentNullException.ThrowIfNull(updatedDocument);
        ArgumentNullException.ThrowIfNull(entityToUpdate);

        // For simplicity, email and JWT access token cannot be updated
        entityToUpdate.FirstName = updatedDocument.FirstName;
        entityToUpdate.LastName = updatedDocument.LastName;
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;
        entityToUpdate.Topics = updatedDocument.Topics?.ConvertAll(t => new Topic
        {
            Name = t,
        });

        return entityToUpdate;
    }

    protected override async Task<User> CreateAsync(ReplicatedUser newDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(newDocument);

        var workspacePk = await _dbContext.Workspaces
            .Where(w => w.ReplicatedDocumentId == newDocument.WorkspaceId)
            .Select(w => w.Id)
            .SingleAsync(cancellationToken);

        return new User
        {
            Id = Provider.Sql.Create(),
            ReplicatedDocumentId = newDocument.Id,
            FirstName = newDocument.FirstName,
            LastName = newDocument.LastName,
            Email = newDocument.Email,
            Role = newDocument.Role,
            JwtAccessToken = newDocument.JwtAccessToken,
            UpdatedAt = newDocument.UpdatedAt,
            IsDeleted = false,
            WorkspaceId = workspacePk,
            Topics = newDocument.Topics?.ConvertAll(t => new Topic { Name = t }),
        };
    }
}
