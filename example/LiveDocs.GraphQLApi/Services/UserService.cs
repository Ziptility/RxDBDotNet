﻿using System.Linq.Expressions;
using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using RT.Comb;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Services;

public class UserService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher)
    : DocumentService<User, ReplicatedUser>(dbContext, eventPublisher)
{
    protected override Expression<Func<User, ReplicatedUser>> ProjectToDocument()
    {
        return user => new ReplicatedUser
        {
            Id = user.ReplicatedDocumentId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            WorkspaceId = user.Workspace!.ReplicatedDocumentId,
            IsDeleted = user.IsDeleted,
            UpdatedAt = user.UpdatedAt,
            Topics = user.Topics == null ? null : user.Topics.ConvertAll(t => t.Name),
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
        entityToUpdate.UpdatedAt = updatedDocument.UpdatedAt;
        entityToUpdate.Topics = updatedDocument.Topics?.Select(t => new Topic
        {
            Name = t,
        })
            .ToList();

        return entityToUpdate;
    }

    protected override User Create(ReplicatedUser newDocument)
    {
        ArgumentNullException.ThrowIfNull(newDocument);

        return new User
        {
            Id = Provider.Sql.Create(),
            ReplicatedDocumentId = newDocument.Id,
            FirstName = newDocument.FirstName,
            LastName = newDocument.LastName,
            Email = newDocument.Email,
            Role = newDocument.Role,
            UpdatedAt = newDocument.UpdatedAt,
            IsDeleted = false,
            WorkspaceId = newDocument.WorkspaceId,
            Topics = newDocument.Topics?.Select(t => new Topic { Name = t, }).ToList(),
        };
    }
}
