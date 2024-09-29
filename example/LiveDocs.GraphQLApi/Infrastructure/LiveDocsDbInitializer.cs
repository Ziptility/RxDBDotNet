using LiveDocs.GraphQLApi.Data;
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

            try
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
            catch
            {
                // ignore this error; the db exists
            }

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
                Name = "Default Workspace",
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
                ReplicatedDocumentId = Guid.NewGuid(),
                Topics = [],
            };

            await dbContext.Workspaces.AddAsync(rootWorkspace);

            var systemAdminReplicatedUser = new ReplicatedUser
            {
                Id = Guid.NewGuid(),
                FirstName = "System",
                LastName = "Admin",
                Email = "systemadmin@livedocs.example.org",
                Role = UserRole.SystemAdmin,
                WorkspaceId = rootWorkspace.ReplicatedDocumentId,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false,
            };

            // Generate a non-expiring JWT token for the system admin user
            // We'll use this in the client app to bootstrap the "logged in" state
            // since we are not supporting username and password login in this example application
            var nonExpiringToken = JwtUtil.GenerateJwtToken(systemAdminReplicatedUser, expires: DateTime.MaxValue);

            var systemAdminUser = new User
            {
                Id = RT.Comb.Provider.Sql.Create(),
                FirstName = systemAdminReplicatedUser.FirstName,
                LastName = systemAdminReplicatedUser.LastName,
                Email = systemAdminReplicatedUser.Email,
                Role = systemAdminReplicatedUser.Role,
                JwtAccessToken = nonExpiringToken,
                WorkspaceId = rootWorkspace.Id,
                UpdatedAt = systemAdminReplicatedUser.UpdatedAt,
                IsDeleted = systemAdminReplicatedUser.IsDeleted,
                ReplicatedDocumentId = systemAdminReplicatedUser.Id,
                Topics = [],
            };

            await dbContext.Users.AddAsync(systemAdminUser);

            await dbContext.SaveChangesAsync();
        }
    }
}
