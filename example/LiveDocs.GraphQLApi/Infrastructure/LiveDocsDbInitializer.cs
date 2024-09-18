﻿using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Security;
using Microsoft.EntityFrameworkCore;

namespace LiveDocs.GraphQLApi.Infrastructure
{
    public static class LiveDocsDbInitializer
    {
        public static async Task InitializeAsync()
        {
            await using var dbContext = new LiveDocsDbContext();

            await dbContext.Database.EnsureCreatedAsync();

            if (!await dbContext.Workspaces.AnyAsync())
            {
                await SeedDataAsync(dbContext);
            }
        }

        private static async Task SeedDataAsync(LiveDocsDbContext dbContext)
        {
            var rootWorkspace = new Workspace
            {
                Id = RT.Comb.Provider.Sql.Create(),
                Name = "LiveDocs Example Org Workspace",
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
                ReplicatedDocumentId = Guid.NewGuid(),
            };

            await dbContext.Workspaces.AddAsync(rootWorkspace);

            var systemAdminReplicatedUser = new ReplicatedUser
            {
                Id = Guid.NewGuid(),
                FirstName = "System",
                LastName = "Admin",
                Email = "systemadmin@livedocs.example.org",
                Role = UserRole.SystemAdmin,
                JwtAccessToken = null,
                WorkspaceId = rootWorkspace.ReplicatedDocumentId,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            var jwtAccessToken = JwtUtil.GenerateJwtToken(systemAdminReplicatedUser, UserRole.SystemAdmin);

            var systemAdminUser = new User
            {
                Id = RT.Comb.Provider.Sql.Create(),
                FirstName = systemAdminReplicatedUser.FirstName,
                LastName = systemAdminReplicatedUser.LastName,
                Email = systemAdminReplicatedUser.Email,
                Role = systemAdminReplicatedUser.Role,
                JwtAccessToken = jwtAccessToken,
                WorkspaceId = rootWorkspace.Id,
                UpdatedAt = systemAdminReplicatedUser.UpdatedAt,
                IsDeleted = systemAdminReplicatedUser.IsDeleted,
                ReplicatedDocumentId = systemAdminReplicatedUser.Id,
            };

            await dbContext.Users.AddAsync(systemAdminUser);

            await dbContext.SaveChangesAsync();
        }
    }
}
