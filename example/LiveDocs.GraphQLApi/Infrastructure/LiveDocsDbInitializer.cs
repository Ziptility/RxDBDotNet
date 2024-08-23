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

            var workspacePk = RT.Comb.Provider.Sql.Create();
            var liveDocsWorkspace = new Workspace
            {
                Id = workspacePk,
                Name = "LiveDocs Org Workspace",
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
                ReplicatedDocumentId = workspacePk,
            };

            await dbContext.Workspaces.AddAsync(liveDocsWorkspace);

            var userPk = RT.Comb.Provider.Sql.Create();
            var systemAdminReplicatedUser = new ReplicatedUser
            {
                Id = userPk,
                FirstName = "System",
                LastName = "Admin",
                Email = "superadmin@livedocs.example.org",
                JwtAccessToken = null,
                WorkspaceId = liveDocsWorkspace.Id,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            var jwtAccessToken = JwtUtil.GenerateJwtToken(systemAdminReplicatedUser, UserRole.SystemAdmin);

            var superAdminUser = new User
            {
                Id = userPk,
                FirstName = "System",
                LastName = "Admin",
                Email = "systemadmin@livedocs.example.org",
                JwtAccessToken = jwtAccessToken,
                WorkspaceId = liveDocsWorkspace.Id,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
                ReplicatedDocumentId = userPk,
            };

            await dbContext.Users.AddAsync(superAdminUser);

            await dbContext.SaveChangesAsync();
        }
    }
}
