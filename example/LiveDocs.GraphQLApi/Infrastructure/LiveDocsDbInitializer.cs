using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Entities;
using LiveDocs.GraphQLApi.Models.Replication;
using LiveDocs.GraphQLApi.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace LiveDocs.GraphQLApi.Infrastructure
{
    public static class LiveDocsDbInitializer
    {
        public static async Task InitializeAsync()
        {
            await using var dbContext = new LiveDocsDbContext();

            await dbContext.Database.EnsureCreatedAsync();

            await SeedDataAsync(dbContext);
        }

        private static async Task SeedDataAsync(LiveDocsDbContext dbContext)
        {
            if (await dbContext.Workspaces.AnyAsync())
            {
                return; // Data has already been seeded
            }

            var liveDocsWorkspace = new Workspace
            {
                Id = RT.Comb.Provider.Sql.Create(),
                Name = "LiveDocs Example Org Workspace",
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
                ReplicatedDocumentId = Guid.NewGuid(),
            };

            await dbContext.Workspaces.AddAsync(liveDocsWorkspace);

            var systemAdminReplicatedUser = new ReplicatedUser
            {
                Id = Guid.NewGuid(),
                FirstName = "System",
                LastName = "Admin",
                Email = "systemadmin@livedocs.example.org",
                JwtAccessToken = null,
                WorkspaceId = liveDocsWorkspace.ReplicatedDocumentId,
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
                JwtAccessToken = jwtAccessToken,
                WorkspaceId = liveDocsWorkspace.Id,
                UpdatedAt = systemAdminReplicatedUser.UpdatedAt,
                IsDeleted = systemAdminReplicatedUser.IsDeleted,
                ReplicatedDocumentId = systemAdminReplicatedUser.Id,
            };

            await dbContext.Users.AddAsync(systemAdminUser);

            await dbContext.SaveChangesAsync();
        }
    }
}
